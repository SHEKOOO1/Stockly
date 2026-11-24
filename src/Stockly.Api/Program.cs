using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using Stockly.Api.Common;
using Stockly.Api.Configuration;
using Stockly.Api.Outbox;
using Stockly.Api.Tenancy;
using Stockly.Application.Auditing;
using Stockly.Application.Common.Errors;
using Stockly.Infrastructure.Outbox;
using Stockly.Infrastructure.Persistence;
using Stockly.Infrastructure.Persistence.Security;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi("openapi", options =>
{
    options.AddDocumentTransformer((document, _, _) =>
    {
        document.Info = new OpenApiInfo
        {
            Title = "Stockly API",
            Version = "v1",
        };
        document.Servers?.Clear();
        return Task.CompletedTask;
    });
});

// The RFC 9457 writer is the single place a problem body is produced.
builder.Services.AddSingleton<ProblemDetailsWriter>();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();


// The tenant-isolation data-access layer. Tenant scope comes from this context and
// nowhere else: never from a header, a route value or a body field
// (AGENTS.md §4). HttpTenantContext reads the authenticated principal's claims
// and nothing else.
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITenantContext, HttpTenantContext>();

// The connection string is a secret (SECURITY_ARCHITECTURE §7). It is resolved once
// at startup, so a missing value aborts the boot rather than failing on the first
// request, and the failure message names the key without ever echoing the value.
var connectionString = DatabaseConnection.Resolve(builder.Configuration);

// The scope interceptor writes the authenticated scope into SESSION_CONTEXT on every physical
// connection EF opens, which is what makes the database-side tenant and warehouse predicates
// able to see it (ADR-0051).
// It is resolved from the request scope through the IServiceProvider overload, NOT captured as
// an instance here: AddDbContext's options delegate runs per scope, but an interceptor
// constructed once at startup would capture a single ITenantContext and write one request's
// tenant into every connection for the process lifetime -- the exact cross-tenant leak this
// component exists to prevent.
builder.Services.AddScoped<TenantScopeConnectionInterceptor>();
builder.Services.AddDbContext<StocklyDbContext>((sp, options) =>
    options.UseSqlServer(connectionString)
        .AddInterceptors(sp.GetRequiredService<TenantScopeConnectionInterceptor>())
        // Required because the query filters capture this scope. Without it EF caches one
        // model per context TYPE and the first tenant to open a connection would decide the
        // filter for the whole process.
        .ReplaceService<Microsoft.EntityFrameworkCore.Infrastructure.IModelCacheKeyFactory, TenantScopeModelCacheKeyFactory>());

// Audit staging. Scoped so entries cannot leak between requests. The collector does
// not save: the caller drains it inside its own transaction so the row commits or
// rolls back with the business change (PROJECT_BLUEPRINT §16 rule 6).
builder.Services.AddScoped<IAuditCollector, AuditCollector>();

builder.Services.AddOutbox(builder.Configuration);

// ADR-0034 route scope. The decision itself lives in RouteScopeResolver; this
// handler is only the bridge from an ASP.NET authorization context to it.
builder.Services.AddSingleton<IAuthorizationHandler, ScopeRequirementHandler>();

// Caller IP for rate limiting and audit attribution. X-Forwarded-For is trusted
// only from configured proxy hops (SECURITY_ARCHITECTURE §6); an empty list means
// the socket address is used, which is the safe default for a direct deployment.
builder.Services.AddSingleton(_ => new ForwardedIpResolver(
    builder.Configuration.GetSection("Security:TrustedProxies").Get<string[]>()));

var app = builder.Build();

app.UseExceptionHandler(errorApp => errorApp.Run(context =>
{
    if (context.Response.HasStarted || context.RequestAborted.IsCancellationRequested)
    {
        return Task.CompletedTask;
    }

    var writer = context.RequestServices.GetRequiredService<ProblemDetailsWriter>();
    var traceId = CorrelationIdMiddleware.Get(context);
    return writer.WriteAsync(context, ApiProblem.Internal(traceId, context.Request.Path.Value));
}));


// Correlation id first, so the traceId in every log line, audit row and problem
// body is the same value.
app.UseMiddleware<CorrelationIdMiddleware>();

// Security headers before anything can produce a response (SECURITY_ARCHITECTURE §12).
app.UseMiddleware<SecurityHeadersMiddleware>();

app.UseAuthorization();

app.MapGet("/health/live", () => Results.Ok(new { status = "live" }));

app.MapGet("/health/ready", async (
    StocklyDbContext db,
    OutboxMonitor monitor,
    OutboxOptions outboxOptions,
    CancellationToken cancellationToken) =>
{
    if (!await db.Database.CanConnectAsync(cancellationToken))
    {
        return Results.Json(
            new { status = "unavailable", database = "unavailable", migrations = "unknown" },
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    var applied = (await db.Database.GetAppliedMigrationsAsync(cancellationToken))
        .ToHashSet(StringComparer.OrdinalIgnoreCase);
    var expected = db.Database.GetMigrations()
        .ToHashSet(StringComparer.OrdinalIgnoreCase);
    var pending = expected.Except(applied).ToArray();

    if (pending.Length > 0)
    {
        return Results.Json(
            new { status = "unavailable", database = "ok", migrations = "pending", pending },
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    var outbox = await monitor.ReadAsync(cancellationToken);
    var outboxStatus = outbox.Describe(outboxOptions);

    if (outboxStatus != "ok")
    {
        return Results.Json(
            new
            {
                status = "unavailable",
                database = "ok",
                migrations = "current",
                outbox = outboxStatus,
                stale = outbox.Stale,
                deadLetter = outbox.DeadLetter,
            },
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    return Results.Ok(new { status = "ready", database = "ok", migrations = "current", outbox = "ok" });
});

app.MapOpenApi("/api-docs/{documentName}.json");

app.MapControllers();

app.Run();

public partial class Program { }

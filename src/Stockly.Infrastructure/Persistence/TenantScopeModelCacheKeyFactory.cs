using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Stockly.Infrastructure.Persistence;

/// <summary>
/// Makes EF Core's model cache vary by the tenant scope the context was built for.
/// <para>
/// <see cref="StocklyDbContext"/> installs query filters that capture the context's own
/// <c>_tenantId</c> and <c>_isPlatformScope</c>. EF Core's default cache key is the context
/// <em>type</em> plus the design-time flag, so the model — including those captured filter
/// values — is built once per type and handed to every later context of that type. The first
/// context to touch the model therefore decides the filter for the whole process.
/// </para>
/// <para>
/// Two concrete failures, both observed rather than theoretical. A platform-scope context built
/// first means every later tenant context runs with <em>no</em> tenant filter at all. A tenant
/// context built first means every later context, including the platform one, runs with that
/// tenant's id baked in — so tenant A's rows are what tenant B's context reads.
/// </para>
/// <para>
/// The database row-level security policy masks this for reads and writes (DATABASE_DESIGN §1.2,
/// ADR-0051), which is precisely why it survived: the ORM layer's third line of defence was
/// silently inert, and the only reason it was not exploitable is that a different layer happens
/// to hold. The documented guarantee is that all three layers exist independently, so the ORM
/// layer has to be correct on its own.
/// </para>
/// <para>
/// <strong>Cost.</strong> A model per distinct (tenant, platform) scope means one model build
/// per tenant per process rather than one per process. That is the deliberate trade for a
/// correct filter: the alternative, <c>EF.Parameter</c>, keeps a single model but makes the
/// tenant id a mutable context parameter that every query depends on being set correctly before
/// first use. A forgotten <c>SetParameter</c> fails open or closed depending on implementation,
/// which is the same class of silent failure this class exists to remove. Model build cost is a
/// measured item, not an assumed one.
/// </para>
/// </summary>
public sealed class TenantScopeModelCacheKeyFactory : IModelCacheKeyFactory
{
    /// <inheritdoc />
    public object Create(DbContext context, bool designTime)
    {
        ArgumentNullException.ThrowIfNull(context);

        // Keyed on the resolved values, not the instance: two tenant contexts for the same
        // tenant share a model, which is the case that actually occurs.
        return context is StocklyDbContext stockly
            ? (ContextType: context.GetType(), TenantId: stockly.CurrentTenantId, IsPlatformScope: stockly.IsPlatformScope, designTime)
            : (ContextType: context.GetType(), TenantId: (Guid?)null, IsPlatformScope: (bool?)null, designTime);
    }
}
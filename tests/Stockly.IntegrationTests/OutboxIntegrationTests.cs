using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Stockly.Api.Outbox;
using Stockly.Api.Configuration;
using Stockly.Domain.Common;
using Stockly.Domain.Modules.Identity;
using Stockly.Domain.Modules.Ops;
using Stockly.Infrastructure.Outbox;
using Stockly.StocklyDbTestSupport;

namespace Stockly.IntegrationTests;

[Collection(SqlServerCollection.Name)]
public sealed class OutboxIntegrationTests
{
    private readonly SqlServerFixture _fixture;

    public OutboxIntegrationTests(SqlServerFixture fixture)
    {
        fixture.RequireServer();
        _fixture = fixture;
    }

    [Fact]
    public async Task Outbox_row_commits_and_rolls_back_with_its_business_change()
    {
        var tenantId = Guid.NewGuid();
        var committedId = Guid.NewGuid();
        var rolledBackId = Guid.NewGuid();
        var committedTenantId = Guid.NewGuid();
        var rolledBackTenantId = Guid.NewGuid();

        await using (var db = _fixture.CreateContext(null))
        {
            await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
            {
                await using var transaction = await db.Database.BeginTransactionAsync();
                db.Tenants.Add(NewTenant(committedTenantId));
                db.OutboxMessages.Add(NewMessage(committedId, committedTenantId));
                await db.SaveChangesAsync();
                await transaction.CommitAsync();
            });
        }

        await using (var db = _fixture.CreateContext(null))
        {
            await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
            {
                await using var transaction = await db.Database.BeginTransactionAsync();
                db.Tenants.Add(NewTenant(rolledBackTenantId));
                db.OutboxMessages.Add(NewMessage(rolledBackId, rolledBackTenantId));
                await db.SaveChangesAsync();
                await transaction.RollbackAsync();
            });
        }

        try
        {
            await using var verify = _fixture.CreateContext(null);
            (await verify.OutboxMessages.AnyAsync(x => x.Id == committedId)).Should().BeTrue();
            (await verify.OutboxMessages.AnyAsync(x => x.Id == rolledBackId)).Should().BeFalse();
            (await verify.Tenants.AnyAsync(x => x.Id == committedTenantId)).Should().BeTrue();
            (await verify.Tenants.AnyAsync(x => x.Id == rolledBackTenantId)).Should().BeFalse();
        }
        finally
        {
            await DeleteAsync(committedId, rolledBackId);
            await DeleteTenantAsync(committedTenantId);
        }
    }

    [Fact]
    public async Task Dispatcher_processes_a_message_once_and_never_redispatchs_it()
    {
        var options = new OutboxOptions { MaxAttempts = 3 };
        var id = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var handler = new RecordingHandler("test.notification");
        var dispatcher = CreateDispatcher(options, handler);

        await InsertAsync(id, tenantId, "test.notification");
        try
        {
            (await dispatcher.ProcessBatchAsync()).Should().Be(1);
            handler.Handled.Should().Equal(id);

            (await dispatcher.ProcessBatchAsync()).Should().Be(0);
            handler.Handled.Should().Equal(id);

            await using var verify = _fixture.CreateContext(null);
            var message = await verify.OutboxMessages.SingleAsync(x => x.Id == id);
            message.Status.Should().Be(OutboxStatus.Processed);
            message.ProcessedAtUtc.Should().NotBeNull();
            message.Attempts.Should().Be(1);
        }
        finally
        {
            await DeleteAsync(id);
        }
    }

    [Fact]
    public async Task Failed_handler_retries_with_backoff_then_dead_letters()
    {
        var options = new OutboxOptions { MaxAttempts = 2 };
        var id = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var dispatcher = CreateDispatcher(options, new FailingHandler("test.notification"));

        await InsertAsync(id, tenantId, "test.notification");
        try
        {
            (await dispatcher.ProcessBatchAsync()).Should().Be(0);
            await using (var verify = _fixture.CreateContext(null))
            {
                var failed = await verify.OutboxMessages.SingleAsync(x => x.Id == id);
                failed.Status.Should().Be(OutboxStatus.Failed);
                failed.Attempts.Should().Be(1);
                failed.NextAttemptAtUtc.Should().BeAfter(DateTime.UtcNow);
                failed.LastError.Should().Be("InvalidOperationException");
            }

            await MakeDueAsync(id);
            (await dispatcher.ProcessBatchAsync()).Should().Be(0);

            var monitor = new OutboxMonitor(_fixture.PlatformPrincipalConnectionString, options);
            var snapshot = await monitor.ReadAsync();
            snapshot.DeadLetter.Should().BeGreaterThanOrEqualTo(1);
            snapshot.IsHealthy(options).Should().BeFalse();

            await using var final = _fixture.CreateContext(null);
            var dead = await final.OutboxMessages.SingleAsync(x => x.Id == id);
            dead.Status.Should().Be(OutboxStatus.DeadLetter);
            dead.Attempts.Should().Be(2);
        }
        finally
        {
            await DeleteAsync(id);
        }
    }

    [Fact]
    public async Task Stale_processing_lease_is_recovered_by_a_new_worker_instance()
    {
        var options = new OutboxOptions();
        var id = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var handler = new RecordingHandler("test.notification");

        await InsertAsync(id, tenantId, "test.notification");
        await SetProcessingAsync(id, DateTime.UtcNow.AddMinutes(-2));
        try
        {
            var restartedWorker = CreateDispatcher(options, handler);
            (await restartedWorker.ProcessBatchAsync()).Should().Be(1);
            handler.Handled.Should().Equal(id);

            await using var verify = _fixture.CreateContext(null);
            var message = await verify.OutboxMessages.SingleAsync(x => x.Id == id);
            message.Status.Should().Be(OutboxStatus.Processed);
        }
        finally
        {
            await DeleteAsync(id);
        }
    }

    [Fact]
    public async Task Unhandled_message_type_is_released_without_losing_the_event()
    {
        var options = new OutboxOptions();
        var id = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var dispatcher = CreateDispatcher(options);

        await InsertAsync(id, tenantId, "test.no_handler_yet");
        try
        {
            (await dispatcher.ProcessBatchAsync()).Should().Be(0);

            await using var verify = _fixture.CreateContext(null);
            var message = await verify.OutboxMessages.SingleAsync(x => x.Id == id);
            message.Status.Should().Be(OutboxStatus.Pending);
            message.Attempts.Should().Be(0);
            message.LockedUntilUtc.Should().BeNull();
        }
        finally
        {
            await DeleteAsync(id);
        }
    }

    [Fact]
    public async Task Readiness_degrades_for_stale_backlog_and_recovers_after_processing()
    {
        var id = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        await InsertAsync(id, tenantId, "test.notifications");
        try
        {
            using (var client = CreateClient())
            {
                using var response = await client.GetAsync("/health/ready");
                response.StatusCode.Should().Be(HttpStatusCode.OK);
            }

            await MakeStaleAsync(id);
            using (var client = CreateClient())
            {
                using var response = await client.GetAsync("/health/ready");
                response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
                using var body = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
                body.RootElement.GetProperty("outbox").GetString().Should().Be("degraded");
                body.RootElement.GetProperty("stale").GetInt32().Should().BeGreaterThanOrEqualTo(1);
            }

            await MarkProcessedAsync(id);
            using (var client = CreateClient())
            {
                using var response = await client.GetAsync("/health/ready");
                response.StatusCode.Should().Be(HttpStatusCode.OK);
            }
        }
        finally
        {
            await DeleteAsync(id);
        }
    }

    [Fact]
    public async Task Readiness_fails_closed_without_a_monitor_connection()
    {
        var monitor = new OutboxMonitor(null, new OutboxOptions());
        var snapshot = await monitor.ReadAsync();

        snapshot.MonitorConfigured.Should().BeFalse();
        snapshot.Describe(new OutboxOptions()).Should().Be("unavailable");
        snapshot.IsHealthy(new OutboxOptions()).Should().BeFalse();
    }

    private HttpClient CreateClient()
    {
        var factory = new StocklyApiFactory(
            _fixture.ConnectionString,
            _fixture.PlatformPrincipalConnectionString);
        return factory.CreateClient();
    }

    private sealed class StocklyApiFactory(string connectionString, string monitorConnectionString)
        : WebApplicationFactory<Program>
    {
        protected override IHost CreateHost(IHostBuilder builder)
        {
            var originalDatabase = Environment.GetEnvironmentVariable(DatabaseConnection.EnvironmentVariable);
            var originalMonitor = Environment.GetEnvironmentVariable(OutboxOptions.MonitorConnectionEnvironmentVariable);
            var originalWorker = Environment.GetEnvironmentVariable(OutboxOptions.WorkerConnectionEnvironmentVariable);
            var originalEnabled = Environment.GetEnvironmentVariable("Outbox__Enabled");
            Environment.SetEnvironmentVariable(DatabaseConnection.EnvironmentVariable, connectionString);
            Environment.SetEnvironmentVariable(OutboxOptions.MonitorConnectionEnvironmentVariable, monitorConnectionString);
            Environment.SetEnvironmentVariable(OutboxOptions.WorkerConnectionEnvironmentVariable, monitorConnectionString);
            Environment.SetEnvironmentVariable("Outbox__Enabled", "false");

            try
            {
                return base.CreateHost(builder);
            }
            finally
            {
                Environment.SetEnvironmentVariable(DatabaseConnection.EnvironmentVariable, originalDatabase);
                Environment.SetEnvironmentVariable(OutboxOptions.MonitorConnectionEnvironmentVariable, originalMonitor);
                Environment.SetEnvironmentVariable(OutboxOptions.WorkerConnectionEnvironmentVariable, originalWorker);
                Environment.SetEnvironmentVariable("Outbox__Enabled", originalEnabled);
            }
        }
    }

    private OutboxDispatcher CreateDispatcher(OutboxOptions options, params IOutboxMessageHandler[] handlers) =>
        new(
            new OutboxCoordinator(_fixture.PlatformPrincipalConnectionString, options),
            options,
            handlers,
            NullLogger<OutboxDispatcher>.Instance);

    private async Task InsertAsync(Guid id, Guid tenantId, string messageType)
    {
        await using var db = _fixture.CreateContext(tenantId);
        db.OutboxMessages.Add(NewMessage(id, tenantId, messageType));
        await db.SaveChangesAsync();
    }

    private async Task MakeDueAsync(Guid id)
    {
        await using var db = _fixture.CreateContext(null);
        var message = await db.OutboxMessages.SingleAsync(x => x.Id == id);
        message.NextAttemptAtUtc = DateTime.UtcNow.AddSeconds(-1);
        await db.SaveChangesAsync();
    }

    private async Task MakeStaleAsync(Guid id)
    {
        await using var db = _fixture.CreateContext(null);
        var message = await db.OutboxMessages.SingleAsync(x => x.Id == id);
        message.CreatedAtUtc = DateTime.UtcNow.AddMinutes(-30);
        await db.SaveChangesAsync();
    }

    private async Task MarkProcessedAsync(Guid id)
    {
        await using var db = _fixture.CreateContext(null);
        var message = await db.OutboxMessages.SingleAsync(x => x.Id == id);
        message.Status = OutboxStatus.Processed;
        message.ProcessedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }

    private async Task SetProcessingAsync(Guid id, DateTime lockedUntil)
    {
        await using var db = _fixture.CreateContext(null);
        var message = await db.OutboxMessages.SingleAsync(x => x.Id == id);
        message.Status = OutboxStatus.Processing;
        message.LockedUntilUtc = lockedUntil;
        await db.SaveChangesAsync();
    }

    private async Task DeleteAsync(params Guid[] ids)
    {
        await using var db = _fixture.CreateContext(null);
        await db.OutboxMessages
            .Where(x => ids.Contains(x.Id))
            .ExecuteDeleteAsync();
    }

    private async Task DeleteTenantAsync(Guid id)
    {
        await using var db = _fixture.CreateContext(null);
        await db.Tenants
            .Where(x => x.Id == id)
            .ExecuteDeleteAsync();
    }

    private static OutboxMessage NewMessage(Guid id, Guid tenantId, string messageType = "test.notification") => new()
    {
        Id = id,
        TenantId = tenantId,
        MessageType = messageType,
        Payload = "{}",
        Status = OutboxStatus.Pending,
        Attempts = 0,
        NextAttemptAtUtc = DateTime.UtcNow,
        CreatedAtUtc = DateTime.UtcNow,
    };

    private static Tenant NewTenant(Guid id) => new()
    {
        Id = id,
        Code = id.ToString("N")[..12],
        Name = "Outbox test tenant",
        Status = TenantStatus.Active,
        TimeZoneId = "UTC",
        DefaultCulture = "en",
        DateFormat = "yyyy-MM-dd",
    };

    private sealed class RecordingHandler(string messageType) : IOutboxMessageHandler
    {
        public string MessageType { get; } = messageType;

        public List<Guid> Handled { get; } = [];

        public Task HandleAsync(ClaimedOutboxMessage message, CancellationToken cancellationToken)
        {
            Handled.Add(message.Id);
            return Task.CompletedTask;
        }
    }

    private sealed class FailingHandler(string messageType) : IOutboxMessageHandler
    {
        public string MessageType { get; } = messageType;

        public Task HandleAsync(ClaimedOutboxMessage message, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("sensitive detail must never be persisted");
    }
}

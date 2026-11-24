using System.Reflection;
using FluentAssertions;
using NetArchTest.Rules;
using Xunit;

namespace Stockly.ArchitectureTests;

/// <summary>
/// The layering rules in <c>TESTING_STRATEGY.md</c> §3.2.
/// <para>
/// These run with no database and no HTTP pipeline, so a layering violation is
/// caught at build time on any machine, rather than in review.
/// </para>
/// </summary>
public sealed class LayeringTests
{
    private static readonly Assembly Domain = typeof(Stockly.Domain.Common.Entity).Assembly;
    private static readonly Assembly Application = typeof(Stockly.Application.Common.Errors.ApiProblem).Assembly;
    private static readonly Assembly Infrastructure = typeof(Stockly.Infrastructure.Persistence.StocklyDbContext).Assembly;
    private static readonly Assembly Api = typeof(Stockly.Api.Common.ProblemDetailsWriter).Assembly;

    [Fact]
    public void Domain_references_no_other_stockly_project()
    {
        var result = Types.InAssembly(Domain)
            .ShouldNot()
            .HaveDependencyOnAny(
                Application.GetName().Name!,
                Infrastructure.GetName().Name!,
                Api.GetName().Name!)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "Stockly.Domain is the innermost layer and must stay dependency-free: " +
            string.Join("; ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Application_does_not_reference_infrastructure_or_api()
    {
        var result = Types.InAssembly(Application)
            .ShouldNot()
            .HaveDependencyOnAny(Infrastructure.GetName().Name!, Api.GetName().Name!)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "Stockly.Application is the portable core; binding to EF or ASP.NET Core " +
            "there would make it untestable without a database or a host: " +
            string.Join("; ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Infrastructure_does_not_depend_on_the_api_project()
    {
        var result = Types.InAssembly(Infrastructure)
            .ShouldNot()
            .HaveDependencyOn(Api.GetName().Name!)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "The data layer must not know about the host that consumes it: " +
            string.Join("; ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Infrastructure_does_not_reference_aspnet_core()
    {
        // The HTTP binding of the tenant scope lives in Stockly.Api precisely so
        // that the data layer never takes a web dependency. A `Microsoft.AspNetCore.*`
        // reference here would mean the DbContext could be read by an unaudited code
        // path in a non-web host.
        var result = Types.InAssembly(Infrastructure)
            .ShouldNot()
            .HaveDependencyOn("Microsoft.AspNetCore")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            string.Join("; ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Only_the_api_project_references_aspnet_core()
    {
        var offenders = new[] { Domain, Application, Infrastructure }
            .Where(assembly => assembly
                .GetReferencedAssemblies()
                .Any(reference => reference.Name!.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal)))
            .Select(assembly => assembly.GetName().Name!)
            .ToArray();

        offenders.Should().BeEmpty();
    }

    [Fact]
    public void Domain_contains_no_persistence_or_web_technology()
    {
        var forbidden = new[]
        {
            "Microsoft.EntityFrameworkCore",
            "Microsoft.AspNetCore",
            "Microsoft.Extensions.Logging",
            "System.Data.SqlClient",
            "Microsoft.Data.SqlClient",
        };

        var offenders = Domain.GetReferencedAssemblies()
            .Select(reference => reference.Name!)
            .Where(name => forbidden.Contains(name, StringComparer.Ordinal))
            .ToArray();

        offenders.Should().BeEmpty();
    }
}
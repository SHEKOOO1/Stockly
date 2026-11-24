using FluentAssertions;
using Xunit;

namespace Stockly.ArchitectureTests;

public sealed class ApiHostExceptionHandlingTests
{
    private static string ProgramSource
    {
        get
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);

            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Stockly.slnx")))
            {
                directory = directory.Parent;
            }

            directory.Should().NotBeNull();
            return File.ReadAllText(Path.Combine(directory!.FullName, "src", "Stockly.Api", "Program.cs"));
        }
    }

    [Fact]
    public void The_exception_handler_fallback_uses_the_controlled_problem_writer()
    {
        var source = ProgramSource;

        source.Should().Contain("app.UseExceptionHandler(errorApp =>");
        source.Should().Contain("GetRequiredService<ProblemDetailsWriter>()");
        source.Should().Contain("ApiProblem.Internal(traceId");
        source.Should().NotContain("AddProblemDetails");
    }

    [Fact]
    public void The_exception_handler_fallback_does_not_write_to_dead_responses()
    {
        var source = ProgramSource;

        source.Should().Contain("context.Response.HasStarted");
        source.Should().Contain("context.RequestAborted.IsCancellationRequested");
    }
}

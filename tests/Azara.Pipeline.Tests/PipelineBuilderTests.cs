using Shouldly;

namespace Azara.Pipeline.Tests;

public class PipelineBuilderTests
{
    [Fact]
    public async Task Build_WithoutMiddleware_InvokesTerminalDirectly()
    {
        var builder = new PipelineBuilder<PipelineContext>();
        var pipeline = builder.Build(_ => Task.FromResult(Result.Success()));

        var result = await pipeline(new PipelineContext());

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task Middlewares_ExecuteInRegistrationOrder()
    {
        var executionOrder = new List<string>();

        var builder = new PipelineBuilder<PipelineContext>();
        builder.Use(async (context, next) =>
        {
            executionOrder.Add("first-before");
            var result = await next(context);
            executionOrder.Add("first-after");
            return result;
        });
        builder.Use(async (context, next) =>
        {
            executionOrder.Add("second-before");
            var result = await next(context);
            executionOrder.Add("second-after");
            return result;
        });

        var pipeline = builder.Build(_ =>
        {
            executionOrder.Add("terminal");
            return Task.FromResult(Result.Success());
        });

        await pipeline(new PipelineContext());

        executionOrder.ShouldBe(["first-before", "second-before", "terminal", "second-after", "first-after"]);
    }

    [Fact]
    public async Task Middleware_CanShortCircuit_WithoutCallingNext()
    {
        var terminalCalled = false;

        var builder = new PipelineBuilder<PipelineContext>();
        builder.Use((_, _) => Task.FromResult(Result.Failure(new Error("blocked", "curto-circuito"))));

        var pipeline = builder.Build(_ =>
        {
            terminalCalled = true;
            return Task.FromResult(Result.Success());
        });

        var result = await pipeline(new PipelineContext());

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("blocked");
        terminalCalled.ShouldBeFalse();
    }

    [Fact]
    public async Task BuiltPipeline_IsReusable_AcrossMultipleExecutions()
    {
        var builder = new PipelineBuilder<PipelineContext>();
        builder.Use(async (context, next) =>
        {
            var quantity = (int)context.Items["quantity"]!;
            return quantity <= 0
                ? Result.Failure(new Error("invalid_quantity", "deve ser positivo"))
                : await next(context);
        });

        var pipeline = builder.Build(_ => Task.FromResult(Result.Success()));

        var failing = new PipelineContext();
        failing.Items["quantity"] = -1;
        var successful = new PipelineContext();
        successful.Items["quantity"] = 5;

        (await pipeline(failing)).IsFailure.ShouldBeTrue();
        (await pipeline(successful)).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task ExceptionInMiddleware_DoesNotCorruptSubsequentExecutions()
    {
        var builder = new PipelineBuilder<PipelineContext>();
        builder.Use(async (context, next) =>
            context.Items.ContainsKey("throw")
                ? throw new InvalidOperationException("boom")
                : await next(context));

        var pipeline = builder.Build(_ => Task.FromResult(Result.Success()));

        var throwingContext = new PipelineContext();
        throwingContext.Items["throw"] = true;

        await Should.ThrowAsync<InvalidOperationException>(() => pipeline(throwingContext));

        var result = await pipeline(new PipelineContext());
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task Use_WithMiddlewareInstance_IsInvoked()
    {
        var builder = new PipelineBuilder<PipelineContext>();
        builder.Use(new TraceMiddleware());

        var pipeline = builder.Build(_ => Task.FromResult(Result.Success()));
        var context = new PipelineContext();

        await pipeline(context);

        context.Items["trace"].ShouldBe("invoked");
    }

    [Fact]
    public async Task Use_Generic_WithParameterlessConstructor_IsInvoked()
    {
        var builder = new PipelineBuilder<PipelineContext>();
        builder.Use<TraceMiddleware>();

        var pipeline = builder.Build(_ => Task.FromResult(Result.Success()));
        var context = new PipelineContext();

        await pipeline(context);

        context.Items["trace"].ShouldBe("invoked");
    }

    private sealed class TraceMiddleware : IPipelineMiddleware<PipelineContext>
    {
        public Task<Result> InvokeAsync(PipelineContext context, PipelineDelegate<PipelineContext> next)
        {
            context.Items["trace"] = "invoked";
            return next(context);
        }
    }
}

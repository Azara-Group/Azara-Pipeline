using Shouldly;

namespace Azara.Pipeline.Commands.Tests;

public class PipelineInvokerTests
{
    [Fact]
    public async Task SendAsync_WithoutBehaviors_InvokesHandler()
    {
        var invoker = new PipelineInvokerBuilder()
            .AddCommand<Ping, int>(new PingHandler())
            .Build();

        var result = await invoker.SendAsync(new Ping(42));

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(42);
    }

    [Fact]
    public async Task SendAsync_RunsBehaviors_OutermostFirst_HandlerLast()
    {
        var log = new List<string>();
        var invoker = new PipelineInvokerBuilder()
            .AddCommand<Ping, int>(
                new PingHandler(),
                new RecordingBehavior(log, "first"),
                new RecordingBehavior(log, "second"))
            .Build();

        await invoker.SendAsync(new Ping(1));

        log.ShouldBe(["first-before", "second-before", "second-after", "first-after"]);
    }

    [Fact]
    public async Task Behavior_CanShortCircuit_WithoutCallingHandler()
    {
        var handlerCalled = false;
        var invoker = new PipelineInvokerBuilder()
            .AddCommand<Ping, int>(
                new TrackingPingHandler(onHandle: () => handlerCalled = true),
                new ShortCircuitBehavior())
            .Build();

        var result = await invoker.SendAsync(new Ping(1));

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("blocked");
        handlerCalled.ShouldBeFalse();
    }

    [Fact]
    public async Task DifferentCommandTypes_DispatchToTheirOwnHandler()
    {
        var invoker = new PipelineInvokerBuilder()
            .AddCommand<Ping, int>(new PingHandler())
            .AddCommand<Greet, string>(new GreetHandler())
            .Build();

        (await invoker.SendAsync(new Ping(7))).Value.ShouldBe(7);
        (await invoker.SendAsync(new Greet("Azara"))).Value.ShouldBe("Olá, Azara!");
    }

    [Fact]
    public async Task SendAsync_WithoutRegisteredHandler_Throws()
    {
        var invoker = new PipelineInvokerBuilder().Build();

        await Should.ThrowAsync<InvalidOperationException>(() => invoker.SendAsync(new Ping(1)));
    }

    [Fact]
    public void AddCommand_RegisteringSameCommandTwice_Throws()
    {
        var builder = new PipelineInvokerBuilder().AddCommand<Ping, int>(new PingHandler());

        Should.Throw<InvalidOperationException>(() => builder.AddCommand<Ping, int>(new PingHandler()));
    }

    [Fact]
    public async Task SendAsync_PropagatesCancellationTokenToContext()
    {
        CancellationToken? observed = null;
        var invoker = new PipelineInvokerBuilder()
            .AddCommand<Ping, int>(new TrackingPingHandler(captureToken: ct => observed = ct))
            .Build();

        using var cts = new CancellationTokenSource();
        await invoker.SendAsync(new Ping(1), cts.Token);

        observed.ShouldBe(cts.Token);
    }

    private sealed record Ping(int Value) : ICommand<int>;

    private sealed class PingHandler : ICommandHandler<Ping, int>
    {
        public Task<Result<int>> HandleAsync(Ping command, CommandContext context) =>
            Task.FromResult(Result<int>.Success(command.Value));
    }

    private sealed class TrackingPingHandler(Action? onHandle = null, Action<CancellationToken>? captureToken = null)
        : ICommandHandler<Ping, int>
    {
        public Task<Result<int>> HandleAsync(Ping command, CommandContext context)
        {
            onHandle?.Invoke();
            captureToken?.Invoke(context.CancellationToken);
            return Task.FromResult(Result<int>.Success(command.Value));
        }
    }

    private sealed class RecordingBehavior(List<string> log, string name) : IPipelineBehavior<Ping, int>
    {
        public async Task<Result<int>> HandleAsync(Ping command, CommandContext context, CommandHandlerDelegate<int> next)
        {
            log.Add($"{name}-before");
            var result = await next();
            log.Add($"{name}-after");
            return result;
        }
    }

    private sealed class ShortCircuitBehavior : IPipelineBehavior<Ping, int>
    {
        public Task<Result<int>> HandleAsync(Ping command, CommandContext context, CommandHandlerDelegate<int> next) =>
            Task.FromResult(Result<int>.Failure(new Error("blocked", "curto-circuito")));
    }

    private sealed record Greet(string Name) : ICommand<string>;

    private sealed class GreetHandler : ICommandHandler<Greet, string>
    {
        public Task<Result<string>> HandleAsync(Greet command, CommandContext context) =>
            Task.FromResult(Result<string>.Success($"Olá, {command.Name}!"));
    }
}

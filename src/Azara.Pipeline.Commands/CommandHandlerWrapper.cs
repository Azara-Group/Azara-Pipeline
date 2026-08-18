namespace Azara.Pipeline.Commands;

/// <summary>
/// Ponte não genérica em <c>TCommand</c> para despachar um comando cujo tipo concreto só é
/// conhecido em tempo de execução. Guardada em um <c>Dictionary&lt;Type, object&gt;</c> pelo
/// <see cref="PipelineInvoker"/> e convertida por cast — não por reflexão — em
/// <see cref="CommandHandlerWrapper{TResult}"/>, porque o chamador de <c>SendAsync&lt;TResult&gt;</c>
/// já garante o <c>TResult</c> correto em tempo de compilação.
/// </summary>
internal abstract class CommandHandlerWrapper<TResult>
{
    public abstract Task<Result<TResult>> HandleAsync(ICommand<TResult> command, CommandContext context);
}

/// <summary>
/// A cadeia de behaviors + handler para um <typeparamref name="TCommand"/> específico,
/// já compilada em um único delegate no momento do registro — nada é montado por chamada.
/// </summary>
internal sealed class CommandHandlerWrapper<TCommand, TResult> : CommandHandlerWrapper<TResult>
    where TCommand : ICommand<TResult>
{
    private readonly Func<TCommand, CommandContext, Task<Result<TResult>>> _chain;

    public CommandHandlerWrapper(Func<TCommand, CommandContext, Task<Result<TResult>>> chain)
    {
        _chain = chain;
    }

    public override Task<Result<TResult>> HandleAsync(ICommand<TResult> command, CommandContext context)
        => _chain((TCommand)command, context);
}

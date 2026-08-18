namespace Azara.Pipeline.Commands;

/// <summary>
/// Registra o handler e os behaviors de cada tipo de comando e compila a cadeia de execução
/// de cada um em <see cref="Build"/>. Sem descoberta automática — v0.2 não depende de DI;
/// isso chega em <c>Azara.Pipeline.DependencyInjection</c>.
/// </summary>
public sealed class PipelineInvokerBuilder
{
    private readonly Dictionary<Type, object> _wrappers = [];

    /// <summary>
    /// Registra o handler e, opcionalmente, os behaviors de <typeparamref name="TCommand"/>,
    /// executados na ordem informada, mais próximo do handler por último.
    /// </summary>
    public PipelineInvokerBuilder AddCommand<TCommand, TResult>(
        ICommandHandler<TCommand, TResult> handler,
        params IPipelineBehavior<TCommand, TResult>[] behaviors)
        where TCommand : ICommand<TResult>
    {
        ArgumentNullException.ThrowIfNull(handler);

        if (!_wrappers.TryAdd(typeof(TCommand), BuildWrapper(handler, behaviors)))
        {
            throw new InvalidOperationException(
                $"Um handler já foi registrado para o comando '{typeof(TCommand).Name}'.");
        }

        return this;
    }

    /// <summary>Compila o invoker. Seguro para reuso e chamadas concorrentes — nada muda depois disso.</summary>
    public IPipelineInvoker Build() => new PipelineInvoker(_wrappers);

    private static CommandHandlerWrapper<TCommand, TResult> BuildWrapper<TCommand, TResult>(
        ICommandHandler<TCommand, TResult> handler,
        IPipelineBehavior<TCommand, TResult>[] behaviors)
        where TCommand : ICommand<TResult>
    {
        Func<TCommand, CommandContext, Task<Result<TResult>>> chain = handler.HandleAsync;

        for (var i = behaviors.Length - 1; i >= 0; i--)
        {
            var behavior = behaviors[i];
            var next = chain;
            chain = (command, context) => behavior.HandleAsync(command, context, () => next(command, context));
        }

        return new CommandHandlerWrapper<TCommand, TResult>(chain);
    }
}

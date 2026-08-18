namespace Azara.Pipeline.Commands;

/// <summary>
/// Implementação padrão de <see cref="IPipelineInvoker"/>. Construída apenas via
/// <see cref="PipelineInvokerBuilder.Build"/>.
/// </summary>
public sealed class PipelineInvoker : IPipelineInvoker
{
    private readonly IReadOnlyDictionary<Type, object> _wrappers;

    internal PipelineInvoker(IReadOnlyDictionary<Type, object> wrappers)
    {
        _wrappers = wrappers;
    }

    public Task<Result<TResult>> SendAsync<TResult>(ICommand<TResult> command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var commandType = command.GetType();
        if (!_wrappers.TryGetValue(commandType, out var wrapper))
        {
            throw new InvalidOperationException($"Nenhum handler registrado para o comando '{commandType.Name}'.");
        }

        var context = new CommandContext(cancellationToken);
        return ((CommandHandlerWrapper<TResult>)wrapper).HandleAsync(command, context);
    }
}

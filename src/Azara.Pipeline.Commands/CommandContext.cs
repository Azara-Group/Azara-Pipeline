namespace Azara.Pipeline.Commands;

/// <summary>
/// <see cref="IPipelineContext"/> especializado para execução de comandos: adiciona um
/// <see cref="CorrelationId"/> para correlacionar logs de um mesmo <see cref="ICommand{TResult}"/>
/// através de handler e behaviors.
/// </summary>
public sealed class CommandContext : PipelineContext
{
    public CommandContext(
        CancellationToken cancellationToken = default,
        IServiceProvider? services = null,
        string? correlationId = null)
        : base(cancellationToken, services)
    {
        CorrelationId = correlationId ?? Guid.NewGuid().ToString("n");
    }

    public string CorrelationId { get; }
}

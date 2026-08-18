namespace Azara.Pipeline;

/// <summary>
/// Implementação padrão de <see cref="IPipelineContext"/>, suficiente para usar a engine
/// de pipeline diretamente, sem depender de pacotes adicionais.
/// </summary>
public class PipelineContext : IPipelineContext
{
    public PipelineContext(CancellationToken cancellationToken = default, IServiceProvider? services = null)
    {
        CancellationToken = cancellationToken;
        Services = services;
    }

    public IDictionary<object, object?> Items { get; } = new Dictionary<object, object?>();

    public CancellationToken CancellationToken { get; }

    public IServiceProvider? Services { get; }
}

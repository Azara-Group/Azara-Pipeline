namespace Azara.Pipeline;

/// <summary>
/// Estado compartilhado entre os componentes de uma execução de pipeline.
/// Equivalente ao <c>HttpContext</c> do ASP.NET Core, mas independente de HTTP.
/// </summary>
public interface IPipelineContext
{
    /// <summary>
    /// Dados arbitrários trocados entre middlewares durante uma única execução.
    /// Equivalente a <c>HttpContext.Items</c>.
    /// </summary>
    IDictionary<object, object?> Items { get; }

    /// <summary>
    /// Token de cancelamento desta execução. É a única fonte de verdade —
    /// middlewares e handlers devem observar este token, nunca receber um separado.
    /// </summary>
    CancellationToken CancellationToken { get; }

    /// <summary>
    /// Provedor de serviços opcional, disponível quando o pipeline é composto via injeção de dependência.
    /// </summary>
    IServiceProvider? Services { get; }
}

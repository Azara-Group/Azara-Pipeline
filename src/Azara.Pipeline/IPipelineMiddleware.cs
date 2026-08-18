namespace Azara.Pipeline;

/// <summary>
/// Um componente da cadeia de pipeline. Decide invocar <c>next</c> para continuar
/// a execução ou retornar um <see cref="Result"/> diretamente para curto-circuitar a cadeia.
/// </summary>
public interface IPipelineMiddleware<TContext>
    where TContext : IPipelineContext
{
    /// <summary>Processa o contexto e decide se chama <paramref name="next"/> ou curto-circuita.</summary>
    Task<Result> InvokeAsync(TContext context, PipelineDelegate<TContext> next);
}

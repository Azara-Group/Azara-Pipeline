namespace Azara.Pipeline;

/// <summary>
/// Representa o próximo passo de uma cadeia de pipeline. Equivalente ao
/// <c>RequestDelegate</c> do ASP.NET Core, generalizado para qualquer <typeparamref name="TContext"/>.
/// </summary>
public delegate Task<Result> PipelineDelegate<TContext>(TContext context)
    where TContext : IPipelineContext;

namespace Azara.Pipeline;

/// <summary>
/// Monta uma cadeia de <see cref="IPipelineMiddleware{TContext}"/> em um único
/// <see cref="PipelineDelegate{TContext}"/> imutável, na ordem em que os componentes
/// foram registrados — equivalente a <c>IApplicationBuilder.Build()</c>.
/// </summary>
public sealed class PipelineBuilder<TContext>
    where TContext : IPipelineContext
{
    private readonly List<Func<PipelineDelegate<TContext>, PipelineDelegate<TContext>>> _components = [];

    /// <summary>Registra um middleware inline.</summary>
    public PipelineBuilder<TContext> Use(Func<TContext, PipelineDelegate<TContext>, Task<Result>> middleware)
    {
        ArgumentNullException.ThrowIfNull(middleware);
        _components.Add(next => context => middleware(context, next));
        return this;
    }

    /// <summary>Registra uma instância já construída de middleware (útil quando ela tem dependências resolvidas por DI).</summary>
    public PipelineBuilder<TContext> Use(IPipelineMiddleware<TContext> middleware)
    {
        ArgumentNullException.ThrowIfNull(middleware);
        return Use((context, next) => middleware.InvokeAsync(context, next));
    }

    /// <summary>
    /// Registra um middleware pelo tipo, instanciado via construtor sem parâmetros.
    /// Middlewares com dependências devem ser registrados via <see cref="Use(IPipelineMiddleware{TContext})"/>
    /// com uma instância resolvida por DI.
    /// </summary>
    public PipelineBuilder<TContext> Use<TMiddleware>()
        where TMiddleware : IPipelineMiddleware<TContext>, new()
        => Use(new TMiddleware());

    /// <summary>
    /// Compila a cadeia registrada em um único delegate, terminando em <paramref name="terminal"/>.
    /// O resultado é seguro para cache e reuso entre chamadas e threads.
    /// </summary>
    public PipelineDelegate<TContext> Build(PipelineDelegate<TContext> terminal)
    {
        ArgumentNullException.ThrowIfNull(terminal);

        var pipeline = terminal;
        for (var i = _components.Count - 1; i >= 0; i--)
        {
            pipeline = _components[i](pipeline);
        }

        return pipeline;
    }
}

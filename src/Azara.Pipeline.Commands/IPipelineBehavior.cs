namespace Azara.Pipeline.Commands;

/// <summary>
/// Um componente da cadeia de execução de um comando específico — o equivalente tipado de
/// <see cref="IPipelineMiddleware{TContext}"/>, mas trabalhando com <see cref="Result{TResult}"/>
/// em vez do <see cref="Result"/> não tipado do núcleo. Decide invocar <c>next</c> para continuar
/// ou retornar um resultado diretamente para curto-circuitar a cadeia.
/// </summary>
public interface IPipelineBehavior<TCommand, TResult>
    where TCommand : ICommand<TResult>
{
    Task<Result<TResult>> HandleAsync(TCommand command, CommandContext context, CommandHandlerDelegate<TResult> next);
}

namespace Azara.Pipeline.Commands;

/// <summary>
/// Processa um <typeparamref name="TCommand"/> e produz seu resultado. Cada comando tem
/// exatamente um handler.
/// </summary>
public interface ICommandHandler<TCommand, TResult>
    where TCommand : ICommand<TResult>
{
    Task<Result<TResult>> HandleAsync(TCommand command, CommandContext context);
}

namespace Azara.Pipeline.Commands;

/// <summary>Envia um comando para seu handler registrado, passando pela cadeia de behaviors.</summary>
public interface IPipelineInvoker
{
    Task<Result<TResult>> SendAsync<TResult>(ICommand<TResult> command, CancellationToken cancellationToken = default);
}

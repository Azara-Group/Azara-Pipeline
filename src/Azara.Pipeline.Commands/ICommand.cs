namespace Azara.Pipeline.Commands;

/// <summary>
/// Marca um tipo como um comando que, quando executado, produz um <see cref="Result{TResult}"/>
/// de <typeparamref name="TResult"/>.
/// </summary>
public interface ICommand<TResult>
{
}

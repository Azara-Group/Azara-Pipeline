namespace Azara.Pipeline;

/// <summary>
/// Descreve uma falha de negócio retornada por um <see cref="Result"/> ou <see cref="Result{T}"/>.
/// Deliberadamente pequeno: não modela hierarquia de tipos de erro — consumidores definem
/// seus próprios <see cref="Code"/>s, ou usam um pacote de extensão para isso.
/// </summary>
public readonly record struct Error(string Code, string Message, IReadOnlyDictionary<string, object?>? Metadata = null)
{
    /// <summary>Mapeia uma exceção não tratada para um <see cref="Error"/> genérico.</summary>
    public static Error FromException(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return new Error("unhandled_exception", exception.Message);
    }
}

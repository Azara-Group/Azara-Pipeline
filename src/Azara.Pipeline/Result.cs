namespace Azara.Pipeline;

/// <summary>
/// Resultado de uma operação sem valor de retorno: sucesso, falha de negócio ou cancelamento.
/// É um <c>readonly struct</c> deliberadamente — é criado em toda invocação de middleware/handler,
/// e evitar alocação no caminho de sucesso importa sob carga.
/// </summary>
public readonly struct Result : IEquatable<Result>
{
    private readonly ResultState _state;
    private readonly Error? _error;

    private Result(ResultState state, Error? error)
    {
        _state = state;
        _error = error;
    }

    /// <summary>A operação foi concluída sem erro.</summary>
    public bool IsSuccess => _state == ResultState.Success;

    /// <summary>A operação falhou por um motivo de negócio — ver <see cref="Error"/>.</summary>
    public bool IsFailure => _state == ResultState.Failure;

    /// <summary>
    /// Cancelamento é um terceiro estado, distinto de falha: não é um erro de negócio,
    /// então não deve ser confundido com <see cref="IsFailure"/>.
    /// </summary>
    public bool IsCancelled => _state == ResultState.Cancelled;

    public Error Error =>
        _error ?? throw new InvalidOperationException(
            $"Result não possui {nameof(Error)} — verifique {nameof(IsFailure)} antes de acessar.");

    /// <summary>Cria um resultado de sucesso.</summary>
    public static Result Success() => new(ResultState.Success, null);

    /// <summary>Cria um resultado de falha de negócio.</summary>
    public static Result Failure(Error error) => new(ResultState.Failure, error);

    /// <summary>Cria um resultado que representa cancelamento — não é uma falha de negócio.</summary>
    public static Result Cancelled() => new(ResultState.Cancelled, null);

    public bool Equals(Result other) => _state == other._state && Nullable.Equals(_error, other._error);

    public override bool Equals(object? obj) => obj is Result other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(_state, _error);

    public static bool operator ==(Result left, Result right) => left.Equals(right);

    public static bool operator !=(Result left, Result right) => !left.Equals(right);
}

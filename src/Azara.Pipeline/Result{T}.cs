namespace Azara.Pipeline;

/// <summary>
/// Resultado de uma operação que produz um valor de tipo <typeparamref name="T"/> em caso de sucesso.
/// Ver <see cref="Result"/> para o racional de ser um <c>readonly struct</c>.
/// </summary>
public readonly struct Result<T> : IEquatable<Result<T>>
{
    private readonly ResultState _state;
    private readonly T? _value;
    private readonly Error? _error;

    private Result(ResultState state, T? value, Error? error)
    {
        _state = state;
        _value = value;
        _error = error;
    }

    /// <summary>A operação foi concluída sem erro e <see cref="Value"/> está disponível.</summary>
    public bool IsSuccess => _state == ResultState.Success;

    /// <summary>A operação falhou por um motivo de negócio — ver <see cref="Error"/>.</summary>
    public bool IsFailure => _state == ResultState.Failure;

    /// <summary>A operação foi cancelada — não é uma falha de negócio.</summary>
    public bool IsCancelled => _state == ResultState.Cancelled;

    public T Value =>
        IsSuccess
            ? _value!
            : throw new InvalidOperationException(
                $"Result<T> não possui {nameof(Value)} — verifique {nameof(IsSuccess)} antes de acessar.");

    public Error Error =>
        _error ?? throw new InvalidOperationException(
            $"Result<T> não possui {nameof(Error)} — verifique {nameof(IsFailure)} antes de acessar.");

    /// <summary>Cria um resultado de sucesso com <paramref name="value"/>.</summary>
    public static Result<T> Success(T value) => new(ResultState.Success, value, null);

    /// <summary>Cria um resultado de falha de negócio.</summary>
    public static Result<T> Failure(Error error) => new(ResultState.Failure, default, error);

    /// <summary>Cria um resultado que representa cancelamento — não é uma falha de negócio.</summary>
    public static Result<T> Cancelled() => new(ResultState.Cancelled, default, null);

    /// <summary>Conveniência para tratar um valor bem-sucedido como <see cref="Result{T}"/> implicitamente.</summary>
    public static implicit operator Result<T>(T value) => Success(value);

    public bool Equals(Result<T> other) =>
        _state == other._state &&
        EqualityComparer<T?>.Default.Equals(_value, other._value) &&
        Nullable.Equals(_error, other._error);

    public override bool Equals(object? obj) => obj is Result<T> other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(_state, _value, _error);

    public static bool operator ==(Result<T> left, Result<T> right) => left.Equals(right);

    public static bool operator !=(Result<T> left, Result<T> right) => !left.Equals(right);
}

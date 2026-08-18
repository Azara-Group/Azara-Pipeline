namespace Azara.Pipeline.Commands;

/// <summary>Representa o próximo passo na cadeia de behaviors de um comando.</summary>
public delegate Task<Result<TResult>> CommandHandlerDelegate<TResult>();

using Azara.Pipeline;

// Sample mínimo: nenhum pacote além de Azara.Pipeline. Nada de Commands, Logging ou DI ainda —
// isso é intencional: o objetivo é mostrar a engine de middleware crua funcionando por si só.

Console.WriteLine("== Azara Pipeline — sample mínimo ==");

var builder = new PipelineBuilder<PipelineContext>();

builder.Use(async (context, next) =>
{
    var quantity = (int)context.Items["quantity"]!;
    Console.WriteLine($"  [trace] iniciando processamento (quantidade={quantity})");
    var result = await next(context);
    Console.WriteLine($"  [trace] finalizado — sucesso={result.IsSuccess}");
    return result;
});

builder.Use((context, next) =>
{
    var quantity = (int)context.Items["quantity"]!;
    if (quantity > 0)
    {
        return next(context);
    }

    Console.WriteLine("  [validação] quantidade inválida — curto-circuitando, handler não será chamado");
    return Task.FromResult(Result.Failure(new Error("invalid_quantity", "Quantidade deve ser maior que zero.")));
});

var pipeline = builder.Build(context =>
{
    context.CancellationToken.ThrowIfCancellationRequested();
    var quantity = (int)context.Items["quantity"]!;
    Console.WriteLine($"  [handler] processando pedido de {quantity} unidade(s)");
    return Task.FromResult(Result.Success());
});

await RunAsync(pipeline, "pedido válido", quantity: 5);
await RunAsync(pipeline, "pedido com quantidade inválida", quantity: -1);

using var cts = new CancellationTokenSource();
await cts.CancelAsync();
await RunAsync(pipeline, "pedido com token já cancelado", quantity: 3, cts.Token);

return;

static async Task RunAsync(
    PipelineDelegate<PipelineContext> pipeline,
    string label,
    int quantity,
    CancellationToken cancellationToken = default)
{
    Console.WriteLine($"\n-- {label} --");
    var context = new PipelineContext(cancellationToken);
    context.Items["quantity"] = quantity;

    try
    {
        var result = await pipeline(context);
        Console.WriteLine(result switch
        {
            { IsSuccess: true } => "Resultado: sucesso",
            { IsCancelled: true } => "Resultado: cancelado",
            _ => $"Resultado: falha — {result.Error.Code} ({result.Error.Message})"
        });
    }
    catch (OperationCanceledException)
    {
        // v0.1 ainda não tem middleware de tratamento de exceções — cancelamento propaga como
        // exceção crua até aqui. Um ExceptionHandlingMiddleware ficará para uma versão futura.
        Console.WriteLine("Resultado: cancelado (propagou como OperationCanceledException)");
    }
}

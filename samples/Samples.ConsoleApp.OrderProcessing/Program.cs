using Azara.Pipeline;
using Azara.Pipeline.Commands;

// Sample da camada de comandos (v0.2): um ICommand, um ICommandHandler e dois IPipelineBehavior
// registrados explicitamente — sem Azara.Pipeline.DependencyInjection ainda (isso é v0.3).

Console.WriteLine("== Azara Pipeline — comandos ==");

var invoker = new PipelineInvokerBuilder()
    .AddCommand<PlaceOrderCommand, OrderConfirmation>(
        new PlaceOrderHandler(),
        new TraceBehavior(),
        new ValidateQuantityBehavior())
    .Build();

await RunAsync(invoker, new PlaceOrderCommand("AZARA-PIPELINE-TSHIRT", Quantity: 3));
await RunAsync(invoker, new PlaceOrderCommand("AZARA-PIPELINE-TSHIRT", Quantity: 0));

return;

static async Task RunAsync(IPipelineInvoker invoker, PlaceOrderCommand command)
{
    Console.WriteLine($"\n-- pedido: {command.Sku} x{command.Quantity} --");
    var result = await invoker.SendAsync(command);

    Console.WriteLine(result.IsSuccess
        ? $"Resultado: sucesso — pedido {result.Value.OrderId} confirmado"
        : $"Resultado: falha — {result.Error.Code} ({result.Error.Message})");
}

internal sealed record PlaceOrderCommand(string Sku, int Quantity) : ICommand<OrderConfirmation>;

internal sealed record OrderConfirmation(string OrderId, int Quantity);

internal sealed class PlaceOrderHandler : ICommandHandler<PlaceOrderCommand, OrderConfirmation>
{
    public Task<Result<OrderConfirmation>> HandleAsync(PlaceOrderCommand command, CommandContext context)
    {
        Console.WriteLine($"  [handler] confirmando pedido de {command.Quantity}x {command.Sku}");
        var confirmation = new OrderConfirmation(OrderId: Guid.NewGuid().ToString("n")[..8], command.Quantity);
        return Task.FromResult(Result<OrderConfirmation>.Success(confirmation));
    }
}

// Behavior mais externo: só observa e loga, sempre chama next().
internal sealed class TraceBehavior : IPipelineBehavior<PlaceOrderCommand, OrderConfirmation>
{
    public async Task<Result<OrderConfirmation>> HandleAsync(
        PlaceOrderCommand command, CommandContext context, CommandHandlerDelegate<OrderConfirmation> next)
    {
        Console.WriteLine($"  [trace] iniciando (correlationId={context.CorrelationId[..8]})");
        var result = await next();
        Console.WriteLine($"  [trace] finalizado — sucesso={result.IsSuccess}");
        return result;
    }
}

// Behavior mais interno: pode curto-circuitar antes do handler ser chamado.
internal sealed class ValidateQuantityBehavior : IPipelineBehavior<PlaceOrderCommand, OrderConfirmation>
{
    public Task<Result<OrderConfirmation>> HandleAsync(
        PlaceOrderCommand command, CommandContext context, CommandHandlerDelegate<OrderConfirmation> next)
    {
        if (command.Quantity > 0)
        {
            return next();
        }

        Console.WriteLine("  [validação] quantidade inválida — curto-circuitando, handler não será chamado");
        return Task.FromResult(Result<OrderConfirmation>.Failure(
            new Error("invalid_quantity", "Quantidade deve ser maior que zero.")));
    }
}

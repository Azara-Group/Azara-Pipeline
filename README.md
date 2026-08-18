# Azara Pipeline

Motor de pipeline genérico para .NET 10, inspirado no middleware do ASP.NET Core (`IApplicationBuilder`/`RequestDelegate`) — mas independente de HTTP. Encadeia qualquer unidade de trabalho (comandos, jobs, etapas de processamento) com suporte nativo a `CancellationToken`, Result pattern e logging opcional via `Microsoft.Extensions.Logging`.

> **Status:** `v0.2.0-preview` — núcleo da engine + Result pattern + camada de comandos. API pode mudar sem aviso até a v1.0. Veja o [roadmap](docs/architecture.md#16-roadmap-de-versões).

```bash
dotnet add package Azara.Pipeline --prerelease
dotnet add package Azara.Pipeline.Commands --prerelease
```

## Por que

Faltava, no ecossistema .NET, a mesma ergonomia do middleware do ASP.NET Core fora do `Microsoft.AspNetCore.Http`: uma cadeia de componentes pequena, componível, sem reflexão no caminho quente, que decide chamar `next()` ou curto-circuitar. A Azara Pipeline generaliza essa ideia e adiciona um Result pattern para tratar falhas de negócio sem depender de exceções.

## Quick start

```csharp
using Azara.Pipeline;

var builder = new PipelineBuilder<PipelineContext>();

builder.Use((context, next) =>
{
    var quantity = (int)context.Items["quantity"]!;
    return quantity > 0
        ? next(context)
        : Task.FromResult(Result.Failure(new Error("invalid_quantity", "Quantidade deve ser maior que zero.")));
});

var pipeline = builder.Build(context =>
{
    Console.WriteLine("processando...");
    return Task.FromResult(Result.Success());
});

var context = new PipelineContext();
context.Items["quantity"] = 5;

var result = await pipeline(context);
Console.WriteLine(result.IsSuccess ? "sucesso" : $"falha: {result.Error.Code}");
```

Veja o exemplo completo em [`samples/Samples.ConsoleApp.Pipeline`](samples/Samples.ConsoleApp.Pipeline/Program.cs).

Se preferir o vocabulário de comando/handler (estilo request-response), use `Azara.Pipeline.Commands`:

```csharp
using Azara.Pipeline;
using Azara.Pipeline.Commands;

public sealed record PlaceOrder(string Sku, int Quantity) : ICommand<string>;

public sealed class PlaceOrderHandler : ICommandHandler<PlaceOrder, string>
{
    public Task<Result<string>> HandleAsync(PlaceOrder command, CommandContext context) =>
        Task.FromResult(Result<string>.Success($"pedido de {command.Quantity}x {command.Sku} confirmado"));
}

var invoker = new PipelineInvokerBuilder()
    .AddCommand<PlaceOrder, string>(new PlaceOrderHandler())
    .Build();

var result = await invoker.SendAsync(new PlaceOrder("SKU-1", 3));
```

Veja o exemplo completo (com behaviors) em [`samples/Samples.ConsoleApp.OrderProcessing`](samples/Samples.ConsoleApp.OrderProcessing/Program.cs).

## Estrutura da solução

```
src/Azara.Pipeline/                     núcleo — engine de pipeline + Result pattern (zero dependências)
src/Azara.Pipeline.Commands/            comando/handler/behavior sobre o núcleo
tests/Azara.Pipeline.Tests/             testes unitários do núcleo (xUnit + Shouldly)
tests/Azara.Pipeline.Commands.Tests/    testes unitários da camada de comandos
samples/                                exemplos de uso
docs/architecture.md                    documento de arquitetura e decisões técnicas
docs/adr/                               Architecture Decision Records
```

Pacotes de logging (`Azara.Pipeline.Logging`) e injeção de dependência (`Azara.Pipeline.DependencyInjection`) estão planejados para a v0.3 — ver [roadmap](docs/architecture.md#16-roadmap-de-versões).

## Build e testes

```bash
dotnet build
dotnet test
dotnet run --project samples/Samples.ConsoleApp.Pipeline
dotnet run --project samples/Samples.ConsoleApp.OrderProcessing
```

## Documentação

- [Arquitetura e decisões técnicas](docs/architecture.md)
- [ADRs](docs/adr/)

## Licença

[MIT](LICENSE)

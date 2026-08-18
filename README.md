# Azara Pipeline

Motor de pipeline genérico para .NET 10, inspirado no middleware do ASP.NET Core (`IApplicationBuilder`/`RequestDelegate`) — mas independente de HTTP. Encadeia qualquer unidade de trabalho (comandos, jobs, etapas de processamento) com suporte nativo a `CancellationToken`, Result pattern e logging opcional via `Microsoft.Extensions.Logging`.

> **Status:** `v0.1.0-preview` — núcleo da engine + Result pattern. API pode mudar sem aviso até a v1.0. Veja o [roadmap](docs/architecture.md#16-roadmap-de-versões).

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

## Estrutura da solução

```
src/Azara.Pipeline/            núcleo — engine de pipeline + Result pattern (zero dependências)
tests/Azara.Pipeline.Tests/    testes unitários (xUnit + Shouldly)
samples/                       exemplos de uso
docs/architecture.md           documento de arquitetura e decisões técnicas
docs/adr/                      Architecture Decision Records
```

Pacotes de comando/handler (`Azara.Pipeline.Commands`), logging (`Azara.Pipeline.Logging`) e injeção de dependência (`Azara.Pipeline.DependencyInjection`) estão planejados a partir da v0.2 — ver [roadmap](docs/architecture.md#16-roadmap-de-versões).

## Build e testes

```bash
dotnet build
dotnet test
dotnet run --project samples/Samples.ConsoleApp.Pipeline
```

## Documentação

- [Arquitetura e decisões técnicas](docs/architecture.md)
- [ADRs](docs/adr/)

## Licença

[MIT](LICENSE)

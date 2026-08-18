# Azara Pipeline — Documento de Arquitetura

**Status:** v0.2.0-preview em andamento — núcleo e camada de comandos implementados.
**Alvo:** .NET 10 · C# 13 · biblioteca open source, sem dependência de ASP.NET Core.

Este documento registra a arquitetura da **Azara Pipeline** e o racional de cada decisão técnica. É atualizado conforme a biblioteca evolui; mudanças de arquitetura relevantes também geram um ADR em [`docs/adr/`](adr/).

## Sumário

1. [Visão e princípios](#1-visão-e-princípios)
2. [Estrutura da solução](#2-estrutura-da-solução)
3. [Camadas e pacotes](#3-camadas-e-pacotes)
4. [Núcleo: contratos do pipeline](#4-núcleo-contratos-do-pipeline)
5. [Result pattern](#5-result-pattern)
6. [Camada de comandos](#6-camada-de-comandos)
7. [Fluxo de execução](#7-fluxo-de-execução)
8. [Tratamento global de exceções](#8-tratamento-global-de-exceções)
9. [CancellationToken — convenção adotada](#9-cancellationtoken--convenção-adotada)
10. [Logging opcional](#10-logging-opcional)
11. [Injeção de dependência](#11-injeção-de-dependência)
12. [Decisões técnicas e justificativas](#12-decisões-técnicas-e-justificativas)
13. [Estratégia de testes](#13-estratégia-de-testes)
14. [Estratégia de benchmarks](#14-estratégia-de-benchmarks)
15. [Empacotamento NuGet](#15-empacotamento-nuget)
16. [Roadmap de versões](#16-roadmap-de-versões)
17. [Estado atual](#17-estado-atual)

---

## 1. Visão e princípios

A Azara Pipeline generaliza o padrão de middleware do ASP.NET Core (`IApplicationBuilder` → `RequestDelegate`) para **qualquer** unidade de trabalho, não apenas requisições HTTP: comandos de aplicação, jobs de fila, etapas de ETL, handlers de mensagens. A ideia central do ASP.NET Core — uma cadeia de componentes que decide invocar `next()` ou curto-circuitar — é ótima e comprovada; o que falta no ecossistema é a mesma ergonomia **fora** do `Microsoft.AspNetCore.Http`.

Princípios que guiam toda decisão abaixo:

- **Núcleo sem dependências.** O pacote central não referencia nada além do BCL. Logging e DI são opt-in, em pacotes separados.
- **Falhas esperadas não são exceções.** Regras de negócio retornam `Result`/`Result<T>`; exceções são reservadas para falhas realmente inesperadas (bugs, I/O, infraestrutura).
- **Composição, não herança.** Middlewares e behaviors são interfaces pequenas; nada de classes base abstratas com estado escondido.
- **Performance é requisito, não otimização tardia.** A cadeia de execução é montada uma vez e cacheada; o caminho quente não usa reflexão nem aloca além do estritamente necessário.
- **API simples primeiro.** Um novo usuário deve conseguir escrever um handler e testá-lo em poucos minutos, sem entender o pipeline inteiro.

## 2. Estrutura da solução

```
AzaraPipeline.sln
├── src/
│   ├── Azara.Pipeline/                          # núcleo: engine + Result (zero deps) — implementado (v0.1)
│   ├── Azara.Pipeline.Commands/                 # ICommand/ICommandHandler/IPipelineBehavior — implementado (v0.2)
│   ├── Azara.Pipeline.Logging/                  # integração com Microsoft.Extensions.Logging — planejado (v0.3)
│   └── Azara.Pipeline.DependencyInjection/       # integração com Microsoft.Extensions.DependencyInjection — planejado (v0.3)
├── tests/
│   ├── Azara.Pipeline.Tests/                    # implementado (v0.1)
│   ├── Azara.Pipeline.Commands.Tests/           # implementado (v0.2)
│   ├── Azara.Pipeline.Logging.Tests/
│   ├── Azara.Pipeline.DependencyInjection.Tests/
│   └── Azara.Pipeline.IntegrationTests/
├── benchmarks/
│   └── Azara.Pipeline.Benchmarks/               # BenchmarkDotNet — planejado (v0.4)
├── samples/
│   ├── Samples.ConsoleApp.Pipeline/             # implementado (v0.1)
│   ├── Samples.ConsoleApp.OrderProcessing/      # implementado (v0.2)
│   ├── Samples.MinimalApi/                      # planejado (v0.4)
│   └── Samples.WorkerService/                   # planejado (v0.4)
├── .github/workflows/publish.yml                # pack + push via NuGet Trusted Publishing
├── Directory.Build.props                        # metadata comum, nullable, langversion, versão do wave
├── Directory.Packages.props                      # central package management
├── assets/icon.png                              # ícone dos pacotes NuGet
├── LICENSE
└── docs/
    ├── architecture.md                          # este documento
    └── adr/                                      # Architecture Decision Records
```

## 3. Camadas e pacotes

| Pacote | Responsabilidade | Depende de | Status |
|---|---|---|---|
| `Azara.Pipeline` | Engine genérica de pipeline (`PipelineBuilder`, `IPipelineMiddleware`), `Result`/`Result<T>`/`Error` | BCL apenas | ✅ v0.1 |
| `Azara.Pipeline.Commands` | Açúcar sintático de comando/handler/behavior sobre a engine core (estilo request/response) | `Azara.Pipeline` | ✅ v0.2 |
| `Azara.Pipeline.Logging` | `LoggingBehavior`, mensagens estruturadas via `LoggerMessage` source generator | `Azara.Pipeline`, `Microsoft.Extensions.Logging.Abstractions` | planejado v0.3 |
| `Azara.Pipeline.DependencyInjection` | `AddAzaraPipeline(...)`, descoberta de handlers/behaviors por assembly scanning | `Azara.Pipeline.Commands`, `Microsoft.Extensions.DependencyInjection.Abstractions` | planejado v0.3 |

Por que `Azara.Pipeline.Commands` é um pacote separado do núcleo: nem todo consumidor quer o vocabulário de "comando/handler" (que remete a CQRS). Alguém que só precisa encadear etapas de validação ou transformação de dados usa a engine crua do `Azara.Pipeline` sem herdar esse vocabulário. Separar em pacotes evita forçar uma opinião de modelagem em quem só quer a primitiva de composição.

Só as bibliotecas `*.Abstractions` da Microsoft são referenciadas — nunca o stack de hosting completo — para que `Logging` e `DependencyInjection` continuem leves e não puxem `Microsoft.Extensions.Hosting` para dentro de aplicações que não o usam.

```mermaid
graph LR
    Core["Azara.Pipeline<br/>engine + Result"]
    Cmd["Azara.Pipeline.Commands"]
    Log["Azara.Pipeline.Logging"]
    DI["Azara.Pipeline.DependencyInjection"]

    Cmd -->|referencia| Core
    Log -->|referencia| Core
    DI -->|referencia| Cmd
    DI -.->|registra behaviors de| Log
```

## 4. Núcleo: contratos do pipeline

A camada mais baixa é deliberadamente pequena — três peças, no espírito de `RequestDelegate`/`IApplicationBuilder`. Implementação em [`src/Azara.Pipeline`](../src/Azara.Pipeline/):

```csharp
public interface IPipelineContext
{
    IDictionary<object, object?> Items { get; }
    CancellationToken CancellationToken { get; }
    IServiceProvider? Services { get; }
}

public delegate Task<Result> PipelineDelegate<TContext>(TContext context)
    where TContext : IPipelineContext;

public interface IPipelineMiddleware<TContext>
    where TContext : IPipelineContext
{
    Task<Result> InvokeAsync(TContext context, PipelineDelegate<TContext> next);
}

public sealed class PipelineBuilder<TContext>
    where TContext : IPipelineContext
{
    public PipelineBuilder<TContext> Use(
        Func<TContext, PipelineDelegate<TContext>, Task<Result>> middleware);

    public PipelineBuilder<TContext> Use(IPipelineMiddleware<TContext> middleware);

    public PipelineBuilder<TContext> Use<TMiddleware>()
        where TMiddleware : IPipelineMiddleware<TContext>, new();

    public PipelineDelegate<TContext> Build(PipelineDelegate<TContext> terminal);
}
```

`PipelineBuilder<TContext>.Build(...)` produz um `PipelineDelegate<TContext>` imutável — equivalente ao `RequestDelegate` compilado pelo `IApplicationBuilder.Build()`. Esse delegate é seguro para cache e reuso entre chamadas e threads, porque nada nele muda depois de construído.

`IPipelineContext.Items` é o "saco de dados" por execução, equivalente ao `HttpContext.Items`: correlação, feature flags de execução, ou dados passados de um middleware para outro sem acoplar suas interfaces.

O pacote também inclui `PipelineContext`, uma implementação padrão de `IPipelineContext`, para que a engine seja usável direto, sem pacotes adicionais.

## 5. Result pattern

```csharp
public readonly record struct Error(
    string Code,
    string Message,
    IReadOnlyDictionary<string, object?>? Metadata = null)
{
    public static Error FromException(Exception ex) =>
        new("unhandled_exception", ex.Message);
}

public readonly struct Result
{
    public bool IsSuccess { get; }
    public bool IsCancelled { get; }
    public Error Error { get; }

    public static Result Success();
    public static Result Failure(Error error);
    public static Result Cancelled();
}

public readonly struct Result<T>
{
    public bool IsSuccess { get; }
    public bool IsCancelled { get; }
    public T Value { get; }
    public Error Error { get; }

    public static Result<T> Success(T value);
    public static Result<T> Failure(Error error);
    public static Result<T> Cancelled();
}
```

Pontos deliberados:

- `Result` e `Result<T>` são **`readonly struct`**, não classes. `Result` é criado em toda invocação de handler/behavior — é caminho quente. Uma struct evita alocação no caso comum de sucesso, o que importa sob carga (menos pressão de GC).
- `IsCancelled` é um terceiro estado, distinto de `IsSuccess`/falha. Cancelamento não é uma falha de negócio; misturar os dois obrigaria o chamador a inspecionar `Error.Code` para saber se foi cancelado, o que é frágil.
- `Error` é um `record struct` com `Code` + `Message` + `Metadata` livre — deliberadamente pequeno. Não modela hierarquia de tipos de erro (validação, not-found, etc.) no core; isso fica para os consumidores definirem `Code`s próprios, ou para um pacote de extensão futuro (`Azara.Pipeline.Validation`).
- `Value`/`Error` lançam `InvalidOperationException` se acessados no estado errado (ex.: `Error` em um `Result` de sucesso) — falha rápido e explícito em vez de retornar `default`/`null` silenciosamente.

## 6. Camada de comandos

Sobre o núcleo, `Azara.Pipeline.Commands` ([`src/Azara.Pipeline.Commands`](../src/Azara.Pipeline.Commands/)) oferece a ergonomia de comando/handler:

```csharp
public interface ICommand<TResult> { }

public sealed class CommandContext : PipelineContext
{
    public string CorrelationId { get; }
}

public interface ICommandHandler<TCommand, TResult>
    where TCommand : ICommand<TResult>
{
    Task<Result<TResult>> HandleAsync(TCommand command, CommandContext context);
}

public delegate Task<Result<TResult>> CommandHandlerDelegate<TResult>();

public interface IPipelineBehavior<TCommand, TResult>
    where TCommand : ICommand<TResult>
{
    Task<Result<TResult>> HandleAsync(
        TCommand command,
        CommandContext context,
        CommandHandlerDelegate<TResult> next);
}

public interface IPipelineInvoker
{
    Task<Result<TResult>> SendAsync<TResult>(
        ICommand<TResult> command,
        CancellationToken cancellationToken = default);
}

public sealed class PipelineInvokerBuilder
{
    public PipelineInvokerBuilder AddCommand<TCommand, TResult>(
        ICommandHandler<TCommand, TResult> handler,
        params IPipelineBehavior<TCommand, TResult>[] behaviors)
        where TCommand : ICommand<TResult>;

    public IPipelineInvoker Build();
}
```

`IPipelineBehavior<TCommand, TResult>` segue o mesmo padrão de composição do `IPipelineMiddleware<TContext>` do núcleo — decide chamar `next()` ou curto-circuitar — mas tipado ao par comando/resultado, porque `PipelineDelegate<TContext>` do núcleo retorna `Result` (não genérico) e não consegue carregar um valor tipado. Por isso a camada de comandos não reaproveita `PipelineBuilder<TContext>` literalmente; ela reimplementa a mesma composição sobre `CommandHandlerDelegate<TResult>`, que pode carregar `TResult`.

Duas decisões aqui refinam o esboço original deste documento, depois de a implementação expor uma opção mais simples:

- **`CommandContext` herda de `PipelineContext`** (a implementação concreta do núcleo) em vez de reimplementar `IPipelineContext` do zero — reuso direto de `Items`/`CancellationToken`/`Services`, só adiciona `CorrelationId`.
- **Sem `ConcurrentDictionary<Type, Delegate>` cacheado em runtime.** A v0.2 não tem descoberta automática de handlers/behaviors (isso é `Azara.Pipeline.DependencyInjection`, v0.3) — o registro é explícito via `PipelineInvokerBuilder.AddCommand<TCommand, TResult>(...)`, que já conhece `TCommand`/`TResult` em tempo de compilação. Isso permite compilar a cadeia inteira (behaviors + handler) em `Build()`, uma única vez, guardada em um `Dictionary<Type, object>` comum. Em `SendAsync<TResult>`, o despacho é só uma busca no dicionário seguida de um **cast** para `CommandHandlerWrapper<TResult>` — não uma chamada reflexiva — porque o `TResult` do chamador já é garantido pelo compilador (`command : ICommand<TResult>`). Zero reflexão no caminho de execução, e falha rápido: comando sem handler registrado lança na primeira chamada, não silenciosamente. Quando a v0.3 trouxer descoberta via assembly scanning, essa mesma ideia de cache por tipo continua válida — só a origem do registro muda, de chamada explícita para reflexão no startup.

O sample [`samples/Samples.ConsoleApp.OrderProcessing`](../samples/Samples.ConsoleApp.OrderProcessing/Program.cs) demonstra um comando com dois behaviors (trace + validação) e os caminhos de sucesso e curto-circuito.

## 7. Fluxo de execução

```mermaid
sequenceDiagram
    participant App as Aplicação
    participant Inv as PipelineInvoker
    participant Exc as ExceptionHandlingMiddleware
    participant Log as LoggingBehavior (opcional)
    participant Beh as Behaviors registrados
    participant H as ICommandHandler

    App->>Inv: SendAsync(command, ct)
    Inv->>Exc: invoke(context)
    Exc->>Log: next(context)
    Log->>Beh: next(context)
    Beh->>H: next(context)
    H-->>Beh: Result<T>
    Beh-->>Log: Result<T>
    Log-->>Exc: Result<T> (+ log estruturado)
    Exc-->>Inv: Result<T> (ou Failure mapeada, se houve exceção)
    Inv-->>App: Result<T>
```

Este fluxo completo (com `ExceptionHandlingMiddleware` e `LoggingBehavior`) entra em vigor a partir da v0.3. Na v0.2, `App → PipelineInvoker → Behaviors registrados → ICommandHandler` já funciona (ver [`samples/Samples.ConsoleApp.OrderProcessing`](../samples/Samples.ConsoleApp.OrderProcessing/Program.cs)), só faltam as bordas de `ExceptionHandlingMiddleware` e `LoggingBehavior`. Na v0.1, a engine crua (`PipelineBuilder<TContext>`) implementa a composição básica sem tipagem de comando: `App → middlewares registrados → terminal` — ver [`samples/Samples.ConsoleApp.Pipeline`](../samples/Samples.ConsoleApp.Pipeline/Program.cs).

## 8. Tratamento global de exceções

> Planejado — ainda não implementado. Hoje, uma exceção lançada por qualquer middleware propaga sem tratamento até o chamador (ver sample).

```csharp
public interface IPipelineExceptionHandler<TContext>
    where TContext : IPipelineContext
{
    // retorna null se este handler não tratou a exceção;
    // o próximo handler registrado assume a decisão.
    ValueTask<Result?> TryHandleAsync(TContext context, Exception exception);
}

public enum ExceptionPolicy
{
    ConvertToFailure,   // default: mapeia para Result.Failure(Error.FromException)
    LogAndRethrow,       // loga e propaga — para infra que precisa ver a exceção crua
    Custom                // delega inteiramente à cadeia de IPipelineExceptionHandler
}
```

Isso espelha o `IExceptionHandler` introduzido no ASP.NET Core 8: uma cadeia de handlers plugáveis, resolvida por DI, em vez de um único `catch` monolítico. `OperationCanceledException` originada do `CancellationToken` do próprio contexto **não** passa por essa cadeia — é tratada à parte (seção 9), porque cancelamento não é uma falha a ser reportada como erro de negócio.

## 9. CancellationToken — convenção adotada

O `CancellationToken` vive em `IPipelineContext.CancellationToken` como fonte única de verdade — o mesmo modelo do `HttpContext.RequestAborted` no ASP.NET Core. `PipelineDelegate<TContext>` e `IPipelineMiddleware<TContext>.InvokeAsync` **não** recebem um `CancellationToken` separado: isso eliminaria a possibilidade de duas fontes divergentes (alguém passar um token diferente do que está no contexto). Ver [`docs/adr/0002-cancellationtoken-single-source.md`](adr/0002-cancellationtoken-single-source.md).

Cancelamento requisitado durante a execução gera `OperationCanceledException`. Até a v0.3 (quando o `ExceptionHandlingMiddleware` existir), essa exceção propaga sem tratamento — o handler final deve observar `context.CancellationToken` explicitamente (`ThrowIfCancellationRequested()`) e o chamador deve tratar `OperationCanceledException`. A partir da v0.3, ela será convertida para `Result.Cancelled()` antes de qualquer `IPipelineExceptionHandler` customizado.

`CommandContext` (v0.2) herda `PipelineContext`, então o mesmo token passado em `IPipelineInvoker.SendAsync(command, cancellationToken)` chega a `context.CancellationToken` dentro de handlers e behaviors — testado em [`PipelineInvokerTests.SendAsync_PropagatesCancellationTokenToContext`](../tests/Azara.Pipeline.Commands.Tests/PipelineInvokerTests.cs).

## 10. Logging opcional

> Planejado para v0.3 — ainda não implementado.

`Azara.Pipeline.Logging` referenciará apenas `Microsoft.Extensions.Logging.Abstractions`. Sem esse pacote instalado, o core não sabe que logging existe — não há `#if` nem dependência condicional, é isolamento por pacote. `LoggingBehavior` usará `[LoggerMessage]` (source generator) em vez de interpolação de string, para evitar alocação quando o nível de log está desabilitado.

## 11. Injeção de dependência

> Planejado para v0.3 — ainda não implementado.

`Azara.Pipeline.DependencyInjection` fará scanning de assembly **apenas no startup**, nunca por chamada. `RegisterHandlersFrom` varrerá a assembly por implementações de `ICommandHandler<,>` e `IPipelineBehavior<,>`; o `PipelineInvoker` em runtime só resolverá tipos já conhecidos e cacheará a cadeia montada — não há reflexão por comando executado.

## 12. Decisões técnicas e justificativas

| Decisão | Alternativa considerada | Por que esta escolha |
|---|---|---|
| Delegate-chain compilado via `PipelineBuilder.Build()` | Resolver e montar a cadeia a cada execução | Montagem uma vez, reuso do delegate — igual ao `RequestDelegate` do ASP.NET Core |
| `Result`/`Result<T>` como `readonly struct` | `class` (como a maioria das libs de Result no NuGet) | `Result` é criado em todo handler/behavior — caminho quente. Struct evita alocação no caso de sucesso |
| Núcleo sem dependência de `Microsoft.Extensions.*` | Acoplar logging/DI diretamente no core, como muitas libs fazem | Mantém o pacote central pequeno, compatível com Native AOT/trimming, e sem risco de conflito de versão em quem consome |
| `CancellationToken` só via `context.CancellationToken` (sem parâmetro duplicado) | Assinatura convencional do BCL com `CancellationToken` como último parâmetro | Uma única fonte de verdade elimina divergência entre o token do contexto e um token passado à parte |
| `Azara.Pipeline.Commands` como pacote separado do core | Um único pacote com tudo | Quem só precisa da engine de middleware genérica não herda vocabulário de CQRS nem tipos que não usa |
| `net10.0` com Nullable/ImplicitUsings habilitados por padrão | Suportar múltiplos TFMs (net8/net9/net10) | Biblioteca nova, sem legado — mirar só na versão atual simplifica manutenção; multi-targeting pode ser revisitado se houver demanda |

## 13. Estratégia de testes

- **Unitários por pacote** (`xUnit` + `Shouldly`): cada middleware/behavior testado isoladamente com um `next` fake.
- **Testes de contrato da engine** (implementados em [`PipelineBuilderTests`](../tests/Azara.Pipeline.Tests/PipelineBuilderTests.cs)): ordem de registro respeitada, curto-circuito sem chamar `next()`, reuso do pipeline compilado entre execuções, exceção em uma execução não corrompe execuções seguintes.
- **Testes de `Result`/`Result<T>`** (implementados em [`ResultTests`](../tests/Azara.Pipeline.Tests/ResultTests.cs)): estados mutuamente exclusivos, acesso inválido lança `InvalidOperationException`, igualdade estrutural, conversão implícita.
- **Testes da camada de comandos** (implementados em [`PipelineInvokerTests`](../tests/Azara.Pipeline.Commands.Tests/PipelineInvokerTests.cs)): despacho para o handler correto por tipo de comando, ordem de execução dos behaviors, curto-circuito sem chamar o handler, comando sem handler registrado lança, registro duplicado do mesmo comando lança, propagação do `CancellationToken` para o `CommandContext`.
- **Testes de integração** (`Azara.Pipeline.IntegrationTests`, planejado v0.3): cenário ponta a ponta com DI real.

## 14. Estratégia de benchmarks

> Planejado para v0.4.

Projeto `Azara.Pipeline.Benchmarks` com **BenchmarkDotNet** e `[MemoryDiagnoser]`, medindo overhead do pipeline vs. chamada direta, alocações por chamada, custo marginal por middleware adicional, e um comparativo informativo com MediatR.

## 15. Empacotamento NuGet

- **Licença:** MIT — `PackageLicenseExpression=MIT` via `Directory.Build.props`. Arquivo [`LICENSE`](../LICENSE) na raiz.
- **Central Package Management** via [`Directory.Packages.props`](../Directory.Packages.props).
- **Ícone:** [`assets/icon.png`](../assets/icon.png), referenciado centralmente em `Directory.Build.props` (`PackageIcon`) e incluído por pacote via `<None Include=".../assets/icon.png" Pack="true" />`.
- **Versionamento em lockstep:** todos os pacotes compartilham a mesma `<Version>` em `Directory.Build.props`, alinhada à "onda" de release do roadmap (`0.1.0-preview`, `0.2.0-preview`, ...), mesmo quando um pacote específico não mudou naquela onda. Simplifica o início do projeto; pode virar versionamento independente por pacote se o histórico justificar.
- **Publicação:** ao contrário do plano original (adiar para v0.9), a publicação real começou já na v0.1 para validar o pipeline de release cedo. `.github/workflows/publish.yml` builda, testa, empacota e publica no NuGet.org via **Trusted Publishing** (token OIDC de curta duração, sem API key armazenada), disparado por tag `v*` ou manualmente. `Azara.Pipeline` já está publicado como [`0.1.0-preview`](https://www.nuget.org/packages/Azara.Pipeline). SourceLink, symbol packages e validação de trimming/AOT continuam para a v0.9 (congelamento de API).
- **Nomes de pacote:** `Azara.Pipeline`, `Azara.Pipeline.Commands`, `Azara.Pipeline.Logging`, `Azara.Pipeline.DependencyInjection`.

## 16. Roadmap de versões

| Versão | Escopo | Critério de saída | Status |
|---|---|---|---|
| **v0.1.0-preview** | Núcleo: `PipelineBuilder`, `IPipelineMiddleware`, `IPipelineContext`, `Result`/`Result<T>`/`Error` | Sample de console rodando um pipeline simples; testes unitários do core passando | ✅ concluído |
| **v0.2.0-preview** | Camada de comandos: `ICommand`, `ICommandHandler`, `IPipelineBehavior`, `PipelineInvoker` com cadeia compilada no registro | Sample de processamento de pedidos com 2+ behaviors | ✅ concluído |
| **v0.3.0-preview** | `Azara.Pipeline.Logging` (LoggerMessage) + `Azara.Pipeline.DependencyInjection` (assembly scanning) + `ExceptionHandlingMiddleware` | Sample com DI completo + logging estruturado visível | planejado |
| **v0.4.0-preview** | Benchmarks publicados, hardening de performance, samples de Minimal API e Worker Service | Benchmark de overhead documentado; zero alocação no caminho feliz confirmada | planejado |
| **v0.9.0-rc** | Congelamento de API, XML docs completos, validação AOT/trimming, SourceLink, symbol packages, ícone, publicação NuGet.org | Revisão de breaking changes concluída; checklist de release fechado | planejado |
| **v1.0.0** | Release estável | Início do contrato de compatibilidade SemVer | planejado |
| **v1.x** | `Azara.Pipeline.Diagnostics` (OpenTelemetry), integração opcional de validação, middlewares prontos (Timeout via `TimeProvider`, Retry) | — | planejado |
| **v2.0** | Registro via source generator para Native AOT completo | — | planejado |

## 17. Estado atual

A v0.1.0-preview está implementada e publicada no NuGet.org: os quatro tipos do núcleo, `Result`/`Result<T>`/`Error`, 17 testes unitários e um sample de console cobrindo sucesso, curto-circuito e cancelamento.

A v0.2.0-preview está implementada: `ICommand`, `CommandContext`, `ICommandHandler`, `IPipelineBehavior`, `IPipelineInvoker`, `PipelineInvokerBuilder`/`PipelineInvoker` com dispatch sem reflexão, 7 testes unitários e o sample de processamento de pedidos com dois behaviors. Duas decisões refinaram o esboço original (`CommandContext` herda `PipelineContext`; cadeia compilada no registro em vez de cache em runtime) — justificadas na seção 6.

Nenhuma decisão de arquitetura foi violada durante a implementação de nenhuma das duas versões.

Próximo passo: v0.3.0-preview (`Azara.Pipeline.Logging`, `Azara.Pipeline.DependencyInjection`, `ExceptionHandlingMiddleware`) — a ser escopada separadamente quando este trabalho for retomado.

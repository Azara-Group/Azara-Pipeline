# ADR 0002 — CancellationToken só via IPipelineContext, sem parâmetro duplicado

**Status:** aceito
**Contexto:** definição do núcleo (v0.1)

## Contexto

A convenção usual do BCL para métodos assíncronos é receber um `CancellationToken` como último parâmetro (reforçada pelo analisador CA2016). A Azara Pipeline, porém, já carrega o token em `IPipelineContext.CancellationToken`, no mesmo espírito de `HttpContext.RequestAborted` no ASP.NET Core.

Expor **também** um parâmetro `CancellationToken` separado em `PipelineDelegate<TContext>` e `IPipelineMiddleware<TContext>.InvokeAsync` criaria duas fontes de verdade: nada impediria um chamador de passar um token diferente do que está no contexto, e um middleware poderia observar o token errado sem perceber.

## Decisão

`PipelineDelegate<TContext>` e `IPipelineMiddleware<TContext>.InvokeAsync` recebem apenas `TContext`. O token é sempre `context.CancellationToken`.

## Consequências

- **A favor:** impossível divergir — existe só uma forma de obter o token de cancelamento da execução corrente.
- **A favor:** assinaturas mais enxutas na cadeia de middleware.
- **Contra:** diverge da convenção usual do BCL; o analisador CA2016 não reconhece esse padrão e seria acionado incorretamente em assinaturas que seguem esse contrato — deve ser suprimido pontualmente (via `.editorconfig` ou `#pragma`) nos pontos onde `TContext` já carrega o token, com comentário explicando o motivo.
- **Reavaliar se:** o feedback da comunidade indicar que a ausência do parâmetro explícito prejudica a legibilidade ou a descoberta via IntelliSense o suficiente para compensar o risco de divergência.

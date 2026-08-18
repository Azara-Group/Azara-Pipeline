# ADR 0001 — Result e Result&lt;T&gt; como readonly struct

**Status:** aceito
**Contexto:** definição do núcleo (v0.1)

## Contexto

`Result` e `Result<T>` são criados em toda invocação de middleware, behavior e handler — são o tipo de retorno do caminho mais quente da biblioteca. A maioria das implementações de Result pattern no ecossistema .NET (FluentResults, ErrorOr, LanguageExt) usa `class`.

## Decisão

`Result` e `Result<T>` são `readonly struct`.

## Consequências

- **A favor:** nenhuma alocação de heap no caso de sucesso, que é o caminho mais comum. Reduz pressão de GC em pipelines de alta taxa de chamadas.
- **Contra:** `Result<T>` carrega `T? Value` mesmo em estado de falha (sempre `default`) — leve overhead de tamanho da struct em comparação a uma referência nula. Aceitável dado que `Error`/`Value` já são pequenos.
- **Contra:** structs maiores copiam por valor a cada `return`/parâmetro; `Result<T>` foi mantido pequeno (estado + valor + `Error?` opcional) para que isso não vire um problema em `T` grandes — usuários com `T` volumoso devem preferir `T` como referência (classe) dentro do `Result<T>`, não `Result<T>` como referência.

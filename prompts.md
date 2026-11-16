# Registro de uso de IA (prompts)

Conforme os combinados da GS, a IA foi usada como **copiloto**. Abaixo estão os prompts enviados,
o que a IA produziu e o que a equipe precisa revisar e saber explicar.

- **Ferramenta:** Claude Code (Anthropic)
- **Data:** 13/11/2026

---

## Prompt 1

> Agora preciso fazer somente a de C#
> *(com o PDF do enunciado da GS CitizenTech anexado)*

**O que a IA fez:**

- Leu o enunciado e levantou os requisitos da disciplina C# Software Development: Web API em C#/.NET
  com POO, interfaces, injeção de dependência, EF Core, LINQ, async/await, testes unitários, Swagger,
  README e evidências (rubrica: POO 3,0 · Web API 2,5 · EF Core + LINQ 1,5 · Async/Await 1,0 · Qualidade 2,0).
- Usou a mesma CitizenTech da equipe (**SafeCity**, já feita no app mobile) e **o mesmo contrato de API
  do mock do app** (`GET /zonas`, `GET /ocorrencias`, `POST /ocorrencias`), para que as partes conversem.
- Criou a solução em camadas (Domain, Application, Infrastructure, Api, Tests) e modelou o domínio:
  `ZonaRisco`, hierarquia `Ocorrencia` (Furto, Roubo, Tentativa, AtitudeSuspeita), objetos de valor
  `Coordenada` e `PrevisaoHoraria`, Factory e Strategy para o cálculo de risco.
- Configurou o EF Core com SQLite (TPH, owned types, conversores, relacionamento 1:N), gerou a migração
  inicial e a carga dos dados **simulados** do mock do app.
- Escreveu 82 testes (xUnit) e rodou todos com sucesso.
- Subiu a API, testou cada rota (inclusive erros), salvou as respostas e tirou prints do Swagger.
- Rodou a versão web do app mobile apontando para esta API e confirmou: o app carregou zonas e relatos
  da API e um relato criado no app chegou com `201 Created`.

---

## O que a equipe deve revisar antes de entregar

- [ ] Criar o repositório `safecity-api` no GitHub, subir o código e conferir o link na Ficha do README.
- [ ] Completar na Ficha o que as demais disciplinas fazem.
- [ ] Atualizar a Ficha do app mobile dizendo que o backend em C# já está pronto (`EXPO_PUBLIC_API_URL`).
- [ ] Se o modelo de ML já gerou previsões reais, publicá-las com `PUT /api/zonas/{id}/previsao`
      (ou trocar o `riscoPorHora` em `dados-iniciais.json`).
- [ ] Rodar `dotnet test` e `dotnet run` em outra máquina para conferir as instruções.
- [ ] Cada integrante saber explicar: as camadas e por que o domínio não conhece o banco; a herança de
      `Ocorrencia` e onde o polimorfismo é usado; o padrão Strategy dos fatores de risco e a injeção de
      dependência; o mapeamento TPH e a migração; um exemplo de LINQ traduzido para SQL; por que tudo é
      `async` e o papel do `CancellationToken`; e por que nenhum dado pessoal é guardado.

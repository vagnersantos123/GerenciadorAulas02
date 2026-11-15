# Validação e melhoria 002 — 15/11/2026

## Planejamento

Verificado com SQLite descartável, sem bancos pessoais:

| Cenário | Resultado automatizado |
| --- | --- |
| Início e término configuráveis | Datas geradas respeitam os limites, inclusive o último dia representável pelo .NET |
| Dias da semana | Interseção dos dias do ano e da matéria |
| Férias e feriados | Julho inteiro e feriado de um dia excluídos, com limites inclusivos |
| Distribuição por matéria | Número de lições por dia respeitado; matérias com calendários independentes |
| Datas originais | Preservadas após estudo, ajuste da data, mudança do ano e reabertura; edição direta recusada |
| Atraso e adiantamento | Estados derivados do andamento, data prevista efetiva e primeiro estudo |
| Geração repetida/concorrente | Uma geração bem-sucedida; demais recusadas sem duplicação ou troca das datas |
| Calendário insuficiente/vazio | Falha sem plano parcial nem incremento da revisão do banco |

Correções: o iterador não avança além de `DateOnly.MaxValue`; a geração deixa de materializar datas depois da capacidade necessária para as lições da matéria.

A data ajustada, quando presente, é a referência efetiva. Uma lição não concluída com data passada aparece como **Atrasada**, inclusive se já houver estudo. **Estudo adiantado** significa que o primeiro estudo registrado ocorreu antes da data prevista; não significa que a lição foi concluída. O modelo não possui data de conclusão e não a infere a partir da última alteração.

## Seleção visual de lições

No registro de estudo, a matéria determina as lições disponíveis. A seleção permite múltiplas lições, pesquisa por número exato ou trecho do título (sem diferenciar maiúsculas e acentos), resumo antes de salvar e filtro para revisar somente as selecionadas.

- **Pendente:** sem estudo registrado e sem conclusão explícita.
- **Em andamento:** possui estudo e não foi concluída explicitamente.
- **Concluída:** marcada explicitamente como concluída; permanece selecionável para revisão.

Pesquisa e filtros preservam a seleção. Cancelar o seletor descarta as alterações daquele seletor. Trocar a matéria em um novo estudo limpa as lições anteriores e filtra o reforço pela nova matéria. A matéria de uma sessão existente fica desabilitada, respeitando a regra já existente de não transferir vínculos.

A lista usa `CollectionView` virtualizada em um `Grid` com espaço delimitado, sem criar um controle para cada lição no formulário. O resumo mostra até cinco itens; a seleção completa pode ser revisada no seletor. A mesma implementação MAUI atende Windows e Android, sem APIs específicas dessas plataformas.

Salvar uma sessão não conclui lições. A mesma lição pode permanecer em várias sessões e datas. Nenhum esquema SQLite, pacote ou integração Google Drive foi alterado.

## Evidências

- **30 testes aprovados:** 19 existentes e 11 cenários novos, nenhum ignorado. Execução final sem avisos do analisador.
- **Windows:** projeto original e cópia isolada compilados sem MSIX, com zero erros e avisos.
- **Android:** tentativa bloqueada por `NETSDK1147` (workload Android compatível com .NET 10 ausente). Não houve instalação ou atualização de pacotes/workloads nesta etapa.
- **Inicialização Windows:** janela `GerenciadorAulas · Validação 002` aberta. Acessibilidade confirmou uma sessão e 50 minutos, correspondentes à cópia do banco fictício anterior.
- **Formulário real, teste temporário na cópia descartável:** os comandos do formulário abriram o seletor, marcaram duas lições, pesquisaram pelo número, filtraram as selecionadas, confirmaram e salvaram uma nova sessão de 25 minutos. Reabrir o armazenamento confirmou a persistência, a sessão anterior de 50 minutos e a ausência de conclusão automática. Editar e cancelar uma seleção alterada manteve as duas lições originais. Resultado: `artifacts/validation-002/ui-smoke-result.json`, com `Passed=true`.
- O seletor foi deixado aberto. Acessibilidade confirmou duas lições **Em andamento**, uma **Pendente**, resumo com duas selecionadas, pesquisa e botão de confirmação. O teste acionou os comandos e fontes de binding no thread da interface; não certifica cliques, toque ou aparência visual. O teste temporário existe apenas em `artifacts`, não no aplicativo versionado, e não se repete ao reabrir se seu resultado já existe.
- O hash do banco fictício anterior foi registrado e conferido. Foi criada uma cópia de preservação em `artifacts/validation-002/preserved-fixture`; o original não foi aberto pelo aplicativo 002.
- Logs e TRX em `artifacts/validation-002`, ignorado pelo Git. Sem commit ou push.

## Teste manual

Use `artifacts/validation-002/Abrir-Teste-002.cmd` para abrir a cópia descartável; ela mantém seus dados sob `validation-data` ao lado do executável e usa cofre de tokens em memória. O executável original continua usando o perfil normal e não deve ser usado para esta rodada fictícia.

Validar manualmente no Windows e, depois de preparar o workload, em aparelho Android:

1. Abrir **Novo estudo**, escolher matéria e acessar **Selecionar / revisar lições**.
2. Marcar várias lições, pesquisar por número/título e revisar somente selecionadas.
3. Cancelar uma seleção modificada; conferir que o resumo anterior permanece.
4. Confirmar a seleção, registrar minutos e salvar. Fechar/reabrir e conferir o histórico.
5. Registrar outro dia para as mesmas lições e conferir que elas não foram concluídas automaticamente.
6. Conferir os três estados, teclado/toque, rolagem, contraste, leitor de tela e tela pequena com centenas de lições.
7. Gerar planejamento fictício com férias de julho e feriados; repetir e testar capacidade insuficiente.

A ferramenta de captura de imagem apresentou `window capture timed out` mesmo após recuperação. A leitura de acessibilidade funcionou; aparência visual e interação física por clique/toque continuam pendentes de validação manual.

## Arquivos versionados alterados ou adicionados

- `src/GerenciadorAulas.Core/EducationService.cs`: limite do calendário e geração sob demanda.
- `src/GerenciadorAulas.Core/LessonSelection.cs`: seleção, pesquisa, estados e descrição do planejamento, sem alteração de dados persistidos.
- `src/GerenciadorAulas.App/EntryForm.cs`: seleção visual no estudo e reforços limitados à matéria escolhida.
- `src/GerenciadorAulas.App/LessonSelectionPage.cs`: lista virtualizada com múltipla seleção, revisão e cancelamento.
- `src/GerenciadorAulas.App/WorkspacePage.cs`: identificação de andamento, atraso e estudo adiantado no planejamento.
- `tests/GerenciadorAulas.Tests/PlanningSelectionTests.cs`: cenários novos de planejamento e seleção com bancos descartáveis.
- `docs/VALIDACAO_002.md`: este relatório e roteiro manual.

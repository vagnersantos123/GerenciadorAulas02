# V004 — Matérias padrão e calendário visual

## Implementação

Trabalho local, sem commit/push. V002/V003 preservadas. O código anterior está copiado em `artifacts/validation-004/validated-source-before`, com diff Git anterior em `before.patch`.

### Catálogo

- Nove matérias iniciais: Português, Matemática, História, Geografia, Ciências, Artes, Inglês, Educação Física e Ensino Religioso.
- Configurações → Editar e organizar catálogo: renomear, subir/descer, remover e adicionar matérias personalizadas. Salvar confirma o conjunto; cancelar descarta. Alterar o catálogo não reescreve as matérias já cadastradas nos alunos.
- Matérias e lições → Selecionar matérias do catálogo: seleção explícita para o aluno/ano ativo. Quantidade de lições em branco até o usuário informar; ritmo e dias individuais por matéria. Mantidos os limites de segurança existentes (1–5000 lições e 1–50 por dia), sem quantidade fixa imposta.
- Criar matéria personalizada continua disponível diretamente no ano. A frequência inclui dias da semana e lições por dia selecionado; o calendário do ano também é respeitado.
- Importação atômica e sem duplicação por vínculo ao item do catálogo ou nome equivalente (acentos, maiúsculas e espaços). Repetir preserva os dados existentes, mesmo após renomear o item. Anos diferentes permanecem independentes.
- O catálogo inicial é virtual até ser salvo. Apenas abrir Configurações não grava dados nem cria matérias em anos antigos ou novos.

### Calendário e interface

- Sete caixas de seleção com nomes em português nos formulários de ano e matéria. Segunda–sexta são o padrão de anos novos; qualquer combinação não vazia é válida, incluindo somente finais de semana.
- Mantido o mesmo `DayOfWeek[]` numérico serializado. Nenhuma conversão destrutiva ou migração SQL: domingo continua 0, sábado 6. Ao salvar, a lista é validada, ordenada e deduplicada; não modifica planos existentes.
- O gerador existente já usa a interseção dos dias do ano com os da matéria e mantém férias/feriados. A V004 não gera ou ajusta planos automaticamente.
- Tema verde e fundo claro preservados. Cores explícitas de texto, campos, placeholders, seletores e pesquisas evitam texto claro em superfícies claras. Seleções têm rótulos e alvos de toque de pelo menos 48 unidades.
- Alarmes/notificações registrados em `docs/PLANEJAMENTO.md`, sem implementação.

## Testes e compatibilidade

- Baseline: 51/51 aprovados antes das alterações.
- V004: **64/64 aprovados**, zero falhas/ignorados. Inclui 13 casos novos para catálogo, nomes equivalentes, repetição concorrente, rollback da importação, matérias personalizadas, independência entre anos, persistência e backup/restauração.
- Todas as 127 combinações não vazias dos sete dias verificadas; seleções vazias/inválidas rejeitadas.
- JSON numérico legado lido com hash do banco inalterado; leitura do catálogo inicial também não grava. Alterar calendário após gerar planejamento preserva os planos originais.
- O SQLite mantém esquema 1: novo tipo JSON `SubjectCatalog` e campo opcional `Subject.CatalogItemId`. Os demais tipos e dados existentes continuam compatíveis. Não houve alteração da integração Google Drive ou atualização de pacotes.
- Primeira execução dos testes novos expôs comparações de arrays por referência nos próprios testes; corrigidas para comparar o conteúdo serializado completo. Nenhum dado da aplicação precisou ser corrigido.
- Windows compilou com zero erros; dois avisos NU1900 de consulta de vulnerabilidades NuGet indisponível. A tentativa inicial sem restauração encontrou assets exclusivos de Android; a restauração do alvo Windows resolveu, sem mudança de código.
- Android: compilação concluída em 18min24s, **zero erros e um aviso XA0033** sobre os cinco componentes da versão do JDK Microsoft; mesmo aviso da V003. Não foi necessária instalação de ferramentas. A restauração Android não apresentou aviso de NuGet.
- APK: `artifacts/apk/GerenciadorAulas-V004-Debug.apk`, **86.124.486 bytes**, SHA-256 `26ABD31C2110135E037ABF416C9124358B984F0BD238963454DF79019A041E50`. Assinaturas v2/v3 verificadas com `apksigner`. Android mínimo API 26 (8.0), alvo API 36, ARM64 e x86_64; sem ARM de 32 bits. Assemblies incorporados, sem fast deployment. O APK V003 anterior permanece intacto.
- Smoke Windows: cópia compilada com **zero erros/avisos**, usando banco descartável copiado da validação V003 e credenciais em memória. O teste temporário acionou os comandos e controles reais: sete caixas, padrão segunda–sexta, mudança para sábado/domingo, salvar/reabrir, renomear/adicionar no catálogo, importar uma matéria com 3 lições e dias sugeridos do ano, reimportar sem duplicar e reabrir os dias da matéria. Resultado `Passed=true`, preservando as quatro sessões anteriores da cópia.
- O teste temporário existe somente em `artifacts/validation-004/isolated-source`; na cópia, foi suprimido apenas o diálogo informativo do resultado da importação para não bloquear a automação. O aplicativo normal mantém esse diálogo. A aparência e o uso humano da V004 ainda precisam de validação manual; Android foi compilado e inspecionado, sem nova execução em celular nesta tarefa.
- Hashes dos arquivos fictícios originais V001/V002/V003 novamente conferidos e intactos. Nenhum banco pessoal foi aberto. Sem alteração do stash, commit ou push; último commit continua `c586798`.

Evidências: `artifacts/validation-004/tests.log`, `test-results/catalog-004.trx`, `windows-build.log`, `windows-smoke-build.log`, `windows-smoke-result.json`, `android-build.log`, `apk-signature.log`, `apk-badging.log`. Para reabrir o ambiente fictício: `artifacts/validation-004/Abrir-Teste-004.cmd`. O smoke roda somente enquanto seu arquivo de resultado não existe.

## Arquivos da V004

- `src/GerenciadorAulas.Core/Models.cs`: catálogo e vínculo opcional de origem.
- `src/GerenciadorAulas.Core/SubjectCatalogService.cs`: catálogo, importação e regras compartilhadas dos dias.
- `src/GerenciadorAulas.Core/JournalStore.cs`: reconhecimento do novo tipo de registro.
- `src/GerenciadorAulas.Core/EducationService.cs`: validação/normalização dos dias ao salvar.
- `src/GerenciadorAulas.App/StudyDaysField.cs`: sete caixas nativas.
- `src/GerenciadorAulas.App/SubjectCatalogPage.cs`: editor e seleção do catálogo.
- `src/GerenciadorAulas.App/EntryForm.cs`: substituição da entrada numérica dos dias.
- `src/GerenciadorAulas.App/WorkspacePage.cs`: acesso ao catálogo e matéria personalizada.
- `src/GerenciadorAulas.App/App.cs`: contraste dos textos e campos.
- `tests/GerenciadorAulas.Tests/CatalogCalendarTests.cs`: cobertura nova.
- `scripts/build-android.ps1`: saída V004 separada e nome do APK configurável.
- `docs/PLANEJAMENTO.md` e este relatório.

## Validação manual recomendada

No Windows e Android, conferir contraste e toque, editar/reordenar/salvar o catálogo, importar duas matérias com quantidades e frequências diferentes, repetir a importação, criar uma personalizada e salvar um ano com dias não consecutivos. Usar somente dados fictícios. Não desinstalar uma versão com dados importantes para contornar eventual conflito de assinatura do APK de desenvolvimento.

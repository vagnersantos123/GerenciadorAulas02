# V003 — Horários e Android

Validação local em 15/11/2026. Sem commit ou push. As alterações V002 previamente aprovadas foram preservadas.

## Sessões e horários

- Novas sessões sugerem o horário local do dispositivo, com precisão de minutos. A data de início continua editável.
- Edição preserva início, duração e estimativas registrados. Sessões antigas sem início continuam sem início; o aplicativo não inventa um horário histórico.
- `TimePicker` e `DatePicker` nativos, durações rápidas de 30/45/50/60/90 minutos e entrada de qualquer duração inteira não negativa que caiba nas datas suportadas. Zero permanece válido para compatibilidade com registros existentes.
- Início + duração calculam data e hora do término. Alterar término recalcula a duração. Um horário final anterior ao inicial, quando a data ainda é a mesma, avança para o dia seguinte. Para sessões com mais dias, a data de término é explícita.
- Valores locais de calendário, sem conversão implícita para UTC ou inferência de fuso/DST de registros históricos.

## Tempo planejado e realizado

- Matéria: estimativa opcional por lição para a geração do planejamento.
- Planejamento: tempo original preservado e estimativa ajustada opcional. Alterar a matéria depois não reescreve as estimativas dos planos já gerados.
- Sessão: estimativa total editável e cópia do tempo originalmente planejado no primeiro salvamento. Alterar o planejamento posteriormente não muda essa cópia.
- A sugestão só é calculada quando todas as lições selecionadas têm planejamento com estimativa completa. Não apresenta um subtotal como se fosse o total.
- Exibe previsto, realizado e diferença com sinal. Mais ou menos tempo é uma descrição, sem julgamento de desempenho.
- Uma sessão com várias lições mantém o tempo total da sessão. Não há divisão presumida entre as lições.
- Histórico e relatório HTML mostram os horários e a comparação. `SessionTimeComparison.Totals` prepara totais por matéria e período da data de início. Sessões sem estimativa não entram na diferença agregada; seus minutos realizados continuam no total geral. Sessões que atravessam a meia-noite pertencem ao período da data de início, sem rateio automático entre dias.

## Persistência e compatibilidade

O SQLite existente usa registros JSON na tabela `Records`. Os campos novos são opcionais: `EstimatedLessonMinutes` na matéria; `EstimatedMinutes` e `RevisedEstimatedMinutes` no planejamento; `EndDate`, `EndTime`, `EstimatedMinutes` e `PlannedMinutes` na sessão.

Não foi necessária mudança de tabelas, colunas ou esquema SQL. `user_version=1` foi mantido. A evolução é aditiva na leitura do JSON, sem migração ou regravação automática dos registros antigos. Os testes verificam JSON legado sem campos novos, hash do banco inalterado na leitura, edição sem inventar horários, preservação dos identificadores e backup/restauração dos dados novos. Integração Google Drive não alterada.

## Testes Windows

- Antes das alterações: **30/30** testes V002 aprovados.
- Depois: **51/51** aprovados, nenhum ignorado; 21 cenários novos incluindo duração, virada de ano, ano bissexto, múltiplos dias, limites, estimativas, totais, JSON legado e backup/restauração.
- Compilação final Windows sem MSIX: **zero erros e avisos**.
- Cópia descartável aberta com banco copiado da V002 e credenciais em memória. Os bancos anteriores permanecem intocados, com hashes conferidos.
- Teste temporário dos comandos e controles reais: horário antigo ausente preservado; novo início local; seleção de duas lições; sugestão de 40 minutos; início em 31/12/2026 às 23:40; duração rápida; término editável e múltiplos dias; salvamento com término em 01/01/2027 às 00:25, 45 minutos realizados, 60 estimados e 40 originalmente planejados.
- Reabertura confirmou os valores, sem alteração das três sessões anteriores da cópia. Resultado em `artifacts/validation-003/windows-smoke-result.json` (`Passed=true`).
- Acessibilidade Windows confirmou os controles, valores e diferença de -15 minutos. Aparência, teclado/toque e ergonomia ainda requerem validação manual; o teste temporário não substitui uso humano.
- Para reabrir a cópia: `artifacts/validation-003/Abrir-Teste-003.cmd`. O teste temporário só roda uma vez, enquanto não existe seu arquivo de resultado.

## Ferramentas Android

Inventário inicial: SDK global .NET 10.0.404 e SDK 9.0.302; MAUI Windows 10; Android SDK com API 34/35; JDK antigo; cerca de 22 GiB livres. Nada foi desinstalado da instalação anterior.

Instalação isolada em `%LOCALAPPDATA%\GA003`:

- SDK .NET **10.0.100**, compatível com `global.json`; ZIP oficial validado por SHA-512.
- Conjunto de workloads estável **10.0.100**, `maui-android` **10.0.0**, ferramentas .NET Android **36.1.2**. Sem novos packs iOS/MacCatalyst. A listagem pode mostrar registros herdados do Visual Studio, que não representam instalação desses packs no diretório isolado.
- Microsoft OpenJDK **21.0.13.0.2**, ZIP oficial validado por SHA-256.
- Android Platform **API 36**, revisão 2; Build Tools **36.0.0**. Arquivos Google conferidos por tamanho/checksum do manifesto oficial; arquivos essenciais novamente conferidos após copiar para o SDK isolado.
- Command-line tools e platform-tools existentes reaproveitados. Nenhuma nova IDE, alteração de ambiente permanente ou remoção de SDK/workload antigo.

O SDK ZIP inicial trazia manifestos RC; foram substituídos pelo conjunto estável antes da compilação. A verificação de assinaturas NuGet foi mantida, com consulta de revogação em modo offline somente durante a instalação isolada para evitar espera de rede. O `sdkmanager` não concluiu a consulta dos manifestos; os dois pacotes necessários foram obtidos diretamente do repositório oficial e completados no SDK local.

Para compilar novamente pelo ambiente isolado:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/build-android.ps1
```

O script não instala ferramentas. Usa `BuildAndroidOnly=true`, sem propagar um framework Android ao núcleo compartilhado, e gera APK Debug com assemblies incorporados, sem depender de fast deployment. Mantém o aplicativo original sem dados fictícios embutidos.

Compilação Android concluída: **zero erros e um aviso XA0033**, em 14min58s. O aviso decorre dos cinco componentes da versão `21.0.13.0.2` do JDK Microsoft, que o detector da ferramenta Android não interpreta; o Java executou normalmente e o APK foi produzido. Dois timeouts do NuGet foram recuperados durante a restauração.

- APK Debug independente: `artifacts/apk/GerenciadorAulas-V003-Debug.apk` (**86.434.656 bytes**).
- SHA-256: `DE72B43734AFC382348638472DFC4269A55BC1847F8FBF95FD5FCCD763C5CE2A`.
- Assinatura conferida com `apksigner`: esquemas v2 e v3 válidos. Chave de desenvolvimento, para testes.
- Metadados conferidos com `aapt2`: pacote `br.com.gerenciadoraulas.app`, Android mínimo **8.0 / API 26**, alvo API 36, arquiteturas **arm64-v8a e x86_64**. Não inclui suporte a celulares exclusivamente ARM de 32 bits.
- SDKs anteriores e Visual Studio preservados. Nenhuma instalação de IDE nova.

Não houve execução Android neste computador: a verificação do emulador indicou ausência de hypervisor e havia apenas cerca de 260–330 MiB de RAM livre em um computador com 4 GiB. Não foi alterada a virtualização. A inicialização e os controles de toque precisam ser validados em um celular compatível, usando dados fictícios; nenhum aparelho foi instalado ou alterado nesta tarefa. O APK não contém o banco de teste Windows. Antes de instalar no celular, confira se o mesmo pacote já contém dados importantes; não desinstale uma versão existente para contornar eventual conflito de assinatura.

Para testar: transfira o APK ao celular compatível, permita a instalação pelo aplicativo usado para abrir o arquivo e abra o GerenciadorAulas. Cadastre dados fictícios e confira início local, durações rápidas, término após meia-noite, estimativa e persistência ao reabrir. Aparência e ergonomia Windows também permanecem para validação manual.

Documentação oficial: [dependências .NET Android](https://learn.microsoft.com/en-us/dotnet/android/getting-started/installation/dependencies), [Microsoft OpenJDK](https://learn.microsoft.com/en-us/java/openjdk/download), [metadados .NET 10](https://builds.dotnet.microsoft.com/dotnet/release-metadata/10.0/releases.json).

## Arquivos da V003

- `src/GerenciadorAulas.Core/Models.cs`: campos opcionais de horários e estimativas.
- `src/GerenciadorAulas.Core/SessionTiming.cs`: cálculos, validação, sugestões, comparação e totais.
- `src/GerenciadorAulas.Core/EducationService.cs`: validações e preservação do tempo original.
- `src/GerenciadorAulas.Core/ReportService.cs`: comparação no relatório HTML.
- `src/GerenciadorAulas.App/SessionTimeFields.cs`: controles de data/hora, durações e estimativa.
- `src/GerenciadorAulas.App/EntryForm.cs`: integração dos horários e estimativas no formulário.
- `src/GerenciadorAulas.App/WorkspacePage.cs`: comparação no histórico e estimativa no planejamento.
- `src/GerenciadorAulas.App/GerenciadorAulas.App.csproj`: opção para compilar apenas Android.
- `tests/GerenciadorAulas.Tests/SessionTimingTests.cs`: testes novos.
- `scripts/build-android.ps1`: compilação Android reproduzível pelo ambiente isolado.
- `docs/VALIDACAO_003.md`: este relatório.

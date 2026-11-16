# Consolidação V002–V004 e V005 — Google opcional

Validação local em Windows em 15/11/2026. Consolidação e publicação da V005 autorizadas pelo proprietário após revisão. **O proprietário confirmou login Google, upload real, listagem e restauração Android → Windows. Windows → Android continua pendente.**

## Versão estável publicada

- Branch inicial `main`, remoto `https://github.com/vagnersantos123/GerenciadorAulas02.git`, sem divergência após `fetch`.
- Revisadas as alterações pendentes e as cópias preservadas das validações anteriores. Arquivos de código, testes, scripts e documentação foram selecionados explicitamente; nenhum SQLite, backup, APK, token ou arquivo temporário foi incluído.
- Antes da publicação: **64/64 testes aprovados**; Windows compilado com **zero erros/avisos**; Android com **zero erros e um aviso XA0033**, já conhecido, sobre o formato da versão do JDK Microsoft. Compilação Android estável: 14min14s.
- [ae6e96d — V002/V003](https://github.com/vagnersantos123/GerenciadorAulas02/commit/ae6e96d): planejamento, seleção visual de lições, horários e comparação de tempos. As duas versões compartilham alterações nos mesmos arquivos; foram consolidadas juntas.
- [9c4a7ef — V004](https://github.com/vagnersantos123/GerenciadorAulas02/commit/9c4a7ef): catálogo de matérias, dias visuais e contraste.
- Push normal `c586798..9c4a7ef`, sem reescrita de histórico. `ls-remote` confirmou `refs/heads/main` em **9c4a7eff4a8306d9ef96c304abab0c2b87659a09**. O stash anterior foi preservado. A confirmação refere-se ao recebimento dos commits; não foi usada como declaração de aprovação dos jobs remotos do GitHub Actions.
- A revisão do index removeu somente uma linha vazia final de um teste antes do segundo commit. Nenhuma funcionalidade foi alterada nessa limpeza.

## Funcionalidades da V005

- Primeira abertura oferece **Conectar com Google** e **Continuar sem conta**. A escolha é armazenada pelo mecanismo seguro; usuários com autorização existente não são convidados a entrar novamente a cada abertura.
- Configurações e Backup Google Drive permitem gerenciar a conta, mostrar nome/e-mail em cache, atualizar a identificação e desconectar. O aplicativo não recebe a senha Google e não usa login como proteção de abertura do programa.
- Windows mantém navegador externo, PKCE S256, state e callback loopback com porta efêmera. Um teste local sem navegador/credenciais confirmou que o listener inicia neste Windows sem alterar ACLs.
- Android implementa `AuthorizationClient` nativo Google Identity Services, com retorno de atividade e consentimento do Google Play Services. Não usa WebView, callback desktop, segredo Web ou troca de código de servidor dentro do APK. Renovação usa a conta identificada e não abre UI em tarefas automáticas; se exigir consentimento, permanece pendente até reconexão explícita.
- Somente `drive.file`. A identificação vem de `Drive about.get`, sem solicitar Gmail, contatos, perfil ou acesso completo ao Drive. Tokens, identificação e configuração sensível usam `SecureStorage`.
- Conectar prepara `GerenciadorAulas/Backups` e identifica a conta, sem upload dos registros educacionais. Backup manual depende do botão de envio; automático depende de consentimento explícito.
- Versões datadas, data/tamanho/dispositivo/situação do último envio; sucesso somente após tamanho/MD5 confirmados pelo Drive. O SHA-256 do arquivo cifrado acompanha a versão e é validado no download quando disponível; o conteúdo continua protegido por AES-GCM e SHA-256 dos arquivos internos.
- Acima de 5 MiB, upload em blocos de 1 MiB, com confirmação de progresso e consulta de recuperação após falha de transporte. URL de sessão é limitada ao endpoint HTTPS do Google; destino externo é recusado antes de enviar o arquivo. Sessões não são persistidas para retomada após reiniciar o aplicativo.
- Automático somente com aplicativo aberto: espera de 30 segundos sem alterações, limite de espera de cinco minutos, intervalo mínimo configurável e prevenção de reenvio da mesma revisão. Falhas permanecem pendentes. A senha de recuperação fica somente em memória e precisa ser informada novamente após reabrir.
- Lista de backups ordenada pelo mais recente. Download e restauração validam integridade/compatibilidade, exigem confirmação, avisam sobre dados locais mais novos e criam cópia de segurança cifrada antes de substituir. A cópia de segurança usa a mesma senha informada nessa restauração. Sem mesclagem; automático é desativado após restaurar.
- Desconectar remove credenciais locais mesmo offline e informa se não conseguiu confirmar revogação remota. Nesse caso, o proprietário pode remover a permissão na conta Google.
- Não houve alteração do esquema SQLite, das funcionalidades educacionais nem instalação de SDK/workload/IDE. Foi adicionada a dependência Android necessária `Xamarin.GooglePlayServices.Auth 121.4.0.1`, que encapsula o SDK Google 21.4.0. A primeira opção de binding, 121.4.0.2, exigia versões transitivas incompatíveis do AndroidX; o ajuste manteve a compatibilidade com o MAUI atual, sem atualização geral dos pacotes.

## Testes e compilações

- **79/79 aprovados**, zero falhas/ignorados: 64 anteriores e 15 casos adicionais. HTTP e tokens são sintéticos; bancos ficam em diretórios descartáveis.
- Cobertura: escolha offline, PKCE/state, renovação e refresh token preservado, escopo recusado, renovação Android sem interação e vinculada à conta, consentimento pendente, desconexão offline, identificação, resposta 401, download incompleto/corrompido, upload grande com progresso recuperado, rejeição de destino externo, agendamento/limitação de envios, internet indisponível e restauração em segundo banco com senha incorreta, dados locais mais novos e cópia de segurança.
- Windows final: **zero erros e avisos**. Cópia de smoke também compilou com zero erros/avisos.
- Android final: **zero erros e um aviso XA0033**, em 16min05s. Corrigidas duas incompatibilidades de compilação no código novo: nome `Activity` ambíguo e builder de revogação abstrato, que deve ser obtido pela fábrica do SDK.
- O aviso do executor sobre `VSTEST_ARTIFACTS_DIRECTORY` não afetou os resultados; o pacote de tooling de testes não foi atualizado nesta tarefa.
- Smoke Windows em cópia isolada: **Passed=true**, quatro sessões fictícias anteriores preservadas. Os comandos reais da interface permitiram continuar sem conta, exibiram a conta fictícia, criaram um backup cifrado pelo botão manual e verificaram sua integridade por HTTP simulado. Zero uploads antes do botão manual; exatamente um upload simulado após o comando.
- A cópia usa credenciais em memória, HTTP inteiramente simulado e login real desativado. Ela não certifica persistência de credenciais entre processos nem autorização real do Google. O aplicativo normal usa `SecureStorage`; esses fluxos precisam ser validados com credenciais verdadeiras.
- Hashes dos arquivos fictícios originais V001/V002/V003/V004 novamente conferidos e intactos. Nenhum banco pessoal foi aberto.

## APK e evidências

- Arquivo: `artifacts/apk/GerenciadorAulas-V005-Debug.apk`.
- Tamanho: **90.020.036 bytes** (~85,9 MiB).
- SHA-256: **0BA014FBBBB018674C8612E018A91C600C6A60D83671E25DF9E68362E8440C34**.
- Assinaturas v2/v3 verificadas com `apksigner`; mesmo certificado da V004. Pacote `br.com.gerenciadoraulas.app`, Android mínimo 8/API 26, alvo API 36, ARM64 e x86_64. Não inclui ARM de 32 bits.
- SHA-1 público do certificado Debug deste APK, para cadastrar o cliente Android: **8C:01:CC:66:3A:B7:78:32:25:E8:22:AC:63:F9:EB:61:49:B1:FD:A3**. Distribuição assinada por outra chave exige o fingerprint correspondente.
- Logs/resultados: `artifacts/validation-005/{baseline-tests,tests,stable-windows-build,stable-android-build,windows-build-final,windows-smoke-build,android-build,apk-signature,apk-badging}.log`, `test-results/google-005.trx` e `windows-smoke-result.json`.
- Para reabrir o ambiente fictício: `artifacts/validation-005/Abrir-Teste-005-Simulado.cmd`. O smoke executa somente enquanto o arquivo de resultado não existe. A aparência e ergonomia continuam sujeitas a validação manual.

## Configuração pendente e limites

Siga [GOOGLE_DRIVE.md](GOOGLE_DRIVE.md): criar projeto Google Cloud, habilitar Drive API, configurar consentimento/usuários de teste, criar cliente desktop e cliente Android no mesmo projeto, com pacote/certificado corretos. Os clientes foram criados pelo proprietário, sem credenciais embutidas no código. No Windows, informe a configuração diretamente no aplicativo; não envie segredos pelo GitHub.

Login, upload, listagem e **Android → Windows** foram confirmados pelo proprietário. Falta validar **Windows → Android**, consentimento/cancelamento e renovação sem interação, usando a mesma conta e somente dados fictícios. O escopo `drive.file` limita a visibilidade ao aplicativo; arquivos de outro projeto não aparecem automaticamente e Google Picker não foi implementado. Não há sincronização simultânea, mesclagem, serviço de backup Android em segundo plano, retenção automática de versões ou garantia de recuperação de sessão de upload após reiniciar. Backups grandes ainda usam buffers em memória.

Alarmes/notificações e manual com imagens permanecem somente no [planejamento futuro](PLANEJAMENTO.md).

## Arquivos alterados na V005

- `.gitignore`: exclui backups temporários do resolvedor de SDK.
- `README.md`, `docs/GOOGLE_DRIVE.md`, `docs/PLANEJAMENTO.md` e este relatório.
- `scripts/build-android.ps1`: saída V005 separada.
- `src/GerenciadorAulas.Core/GoogleDrive.cs`: sessão opcional, abstração nativa, identificação, integridade, upload em blocos e agendamento.
- `src/GerenciadorAulas.App/GoogleAccountPage.cs`: boas-vindas e gerenciamento de conta.
- `src/GerenciadorAulas.App/App.cs`: oferta inicial e carregamento da configuração automática.
- `src/GerenciadorAulas.App/MauiProgram.cs`, `GerenciadorAulas.App.csproj`, `Platforms/Android/MainActivity.cs` e `Platforms/Android/AndroidGoogleAuthorization.cs`: integração nativa.
- `src/GerenciadorAulas.App/WorkspacePage.cs`: controles e consentimento de backup/restauração.
- `tests/GerenciadorAulas.Tests/GoogleIntegrationTests.cs`: testes novos.

A V005 está autorizada para consolidação na main após testes, compilações e revisão do conteúdo staged. A V006 será mantida local até revisão.

## Revisão final para consolidação

O proprietário confirmou login, upload real, listagem e restauração Android → Windows. Windows → Android permanece pendente de confirmação manual com dados fictícios.

Reexecução da suíte V005: 79/79 aprovados. Windows: zero erros, com quatro avisos NU1900 por indisponibilidade da consulta de vulnerabilidades do NuGet naquela tentativa. Android: snapshot exato do index compilado com zero erros e um aviso conhecido XA0033, em 20min45s, usando MSBuild sequencial sem servidores compartilhados. A primeira execução concorrente conflitou nos arquivos intermediários; uma tentativa posterior não encerrou e foi interrompida antes da execução sequencial bem-sucedida.

APK da revisão: artifacts/apk/GerenciadorAulas-V005-Review-Debug.apk. SHA-256: 82BA1C190619EBF9B5A50EE5F3FC6B300B13F1D46FEA8FB873E0263F7BBC8997. Assinatura verificada por apksigner, código 0, mesmo certificado Debug anterior (SHA-1 8C:01:CC:66:3A:B7:78:32:25:E8:22:AC:63:F9:EB:61:49:B1:FD:A3). O APK validado anteriormente foi preservado, sem sobrescrita.

Revisão de exclusões: nenhum SQLite, backup, APK, JSON OAuth, token ou keystore no conjunto selecionado. As regras de ignore foram conferidas com nomes representativos; as configurações reais de Downloads e o ambiente isolado em artifacts não são versionados. Hashes do banco fictício e resultado da V004 permanecem iguais ao baseline. Stash preservado. Logs da revisão ficam em artifacts/consolidation-005 e artifacts/v005-review-*.log.

A V006 tem testes e correção de timeout separados, sem inclusão no commit V005. Não houve alteração de esquema nem de funcionalidades educacionais nesta consolidação.

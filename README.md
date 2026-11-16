# GerenciadorAulas

Organização familiar dos estudos, com funcionamento local offline. Reconstrução em C#, .NET 10 LTS e MAUI 10 para Android e Windows. O aplicativo antigo foi removido dos arquivos versionados; o histórico Git foi preservado. Não há importação automática do banco antigo.

## Estado desta entrega

- V002–V004 consolidadas e publicadas em `main` (`9c4a7ef`); V004 validada manualmente no Android pelo proprietário.
- V005 em desenvolvimento local para revisão, sem commit/push. **79 testes automatizados aprovados**, preservando os 64 anteriores, com bancos descartáveis e HTTP simulado.
- Planejamento confiável, seleção visual de lições, horários e estimativas, catálogo editável de matérias e seleção visual dos dias no Windows e Android.
- Backup local criptografado e restauração com integridade, confirmação e cópia de segurança.
- Google opcional: PKCE no Windows e autorização nativa Google Identity Services no Android; funcionamento offline preservado. **Login, upload, listagem e restauração Android → Windows confirmados pelo proprietário. Windows → Android permanece pendente de teste manual.**
- Backup Drive manual e automático com consentimento, versões, metadados e integridade. Não há sincronização simultânea nem mesclagem automática.
- Relatórios HTML imprimíveis; no Windows, use a impressão do navegador para salvar como PDF.
- Consulte [configuração Google e testes reais pendentes](docs/GOOGLE_DRIVE.md) e [relatório V005](docs/VALIDACAO_005.md).
## Primeiro uso

1. Escolha conectar com Google ou continuar sem conta. Depois, em **Alunos**, adicione o aluno e salve.
2. Em **Anos letivos**, configure datas, dias da semana, feriados e férias (inclua julho, se aplicável).
3. Em **Matérias e lições**, selecione matérias do catálogo ou crie uma personalizada, com quantidade e frequência individuais. Edite o catálogo nas Configurações.
4. Em **Planejamento**, gere as datas. Falta de capacidade no calendário não gera um plano parcial. Uma segunda geração não sobrescreve o original.
5. Em **Registrar estudo**, selecione visualmente a matéria e uma ou várias lições. A mesma lição pode aparecer em vários dias; uma sessão pode conter várias lições.
6. Marque a conclusão explicitamente em **Matérias e lições → Lições → editar**. Encerrar a sessão não conclui lições.
7. Registre dificuldades e use **Registrar reforço** para manter o vínculo. A superação não apaga o histórico.
8. No menu **Opções** de estudos, atividades, dificuldades e ocorrências, use **Anexar arquivo**. Não são exigidas permissões amplas de armazenamento Android.
9. Exporte um backup local antes de desinstalar ou trocar de aparelho.

As telas usam o aluno e o ano selecionados no cabeçalho. Exclusões de registros referenciados são recusadas. Uma matéria sem histórico pode ser excluída junto das lições geradas. Anexos são deduplicados por SHA-256, fora do SQLite. Arquivos órfãos não são apagados automaticamente nesta versão.

## Windows: preparar, executar e publicar

Em Windows 10 1809+ / Windows 11, instale com autorização o .NET SDK 10 e as ferramentas MAUI/Windows SDK compatíveis (Visual Studio 2026 com desenvolvimento .NET MAUI é uma opção). Confirme `dotnet --info`.

```powershell
dotnet workload install maui-windows --version 10.0.100
dotnet test tests/GerenciadorAulas.Tests
dotnet build src/GerenciadorAulas.App -f net10.0-windows10.0.19041.0 -p:BuildWindowsOnly=true -r win-x64
dotnet run --project src/GerenciadorAulas.App -f net10.0-windows10.0.19041.0 -p:BuildWindowsOnly=true -r win-x64
dotnet publish src/GerenciadorAulas.App -f net10.0-windows10.0.19041.0 -p:BuildWindowsOnly=true -c Release -r win-x64 --self-contained true -p:WindowsAppSDKSelfContained=true
```

O projeto define `WindowsPackageType=None` e perfil `Windows Machine` com `commandName=Project`. A saída de publicação é uma pasta; distribua a pasta completa, não apenas o `.exe`. Não há MSIX nesta versão. Os comandos Windows devem ser executados e validados em Windows antes de distribuir o aplicativo.

## Android: preparar e instalar o APK

Pré-requisitos: SDK .NET 10, workload MAUI Android estável, JDK 21 e Android SDK com plataforma API 36, build-tools 36.0.0 e platform-tools. Instale somente ferramentas que você autorizar. O SDK e workloads não são instalados automaticamente pelo aplicativo.

```sh
dotnet workload install maui-android --version 10.0.100
dotnet test tests/GerenciadorAulas.Tests
dotnet build src/GerenciadorAulas.App -f net10.0-android -p:BuildAndroidOnly=true -c Debug \
  -p:AndroidSdkDirectory="$ANDROID_HOME" -p:JavaSdkDirectory="$JAVA_HOME"
adb devices
adb -s SERIAL install -r src/GerenciadorAulas.App/bin/Debug/net10.0-android/br.com.gerenciadoraulas.app-Signed.apk
adb -s SERIAL shell monkey -p br.com.gerenciadoraulas.app 1
```

Android mínimo: API 26 (Android 8). O APK Debug usa chave de desenvolvimento, não assinatura de distribuição. Para publicação definitiva, configure sua própria chave fora do Git. Testes de regras não exigem emulador. Não substitua a instalação antiga sem antes exportar seu banco: esta versão usa novo identificador e novo diretório de dados.

Neste ambiente de nuvem já preparado:

```sh
source /workspace/.onboarding/activate.sh
export JAVA_HOME=/workspace/.onboarding/java/jdk-21.0.9+10
export PATH="$JAVA_HOME/bin:$PATH"
cd /workspace/GerenciadorAulas02
dotnet test tests/GerenciadorAulas.Tests
dotnet build src/GerenciadorAulas.App -f net10.0-android -p:AndroidSdkDirectory="$ANDROID_HOME" -p:JavaSdkDirectory="$JAVA_HOME" -m:2
```

O helper antigo de onboarding ainda aponta para JDK17/.NET8: não o use para compilar o novo projeto sem as sobrescritas acima. O rascunho/publicação do ambiente não foi alterado nesta tarefa. Se o sandbox bloquear o mutex `NuGet-Migrations`, use o mecanismo de execução com permissão apropriada.

## Arquitetura

`src/GerenciadorAulas.Core`: domínio, validação, calendário, persistência, anexos, relatórios, criptografia e protocolo Drive. Independente de MAUI. `src/GerenciadorAulas.App`: shell, formulários, MVVM com CommunityToolkit, seleção de arquivos e armazenamento seguro. `tests`: xUnit com bancos descartáveis e servidor HTTP simulado.

O SQLite armazena registros tipados em JSON com índices, unicidade e chaves estrangeiras relacionais para propriedade e ligações entre registros. A migração transacional `user_version=1` cria o esquema; versões futuras desconhecidas são recusadas. As gravações compostas e geração de lições/planejamento são transacionais. Comandos aguardam a persistência e bloqueiam cliques concorrentes. Logs de diagnóstico registram somente o tipo da falha, sem conteúdo pessoal.

Consulte [backup e restauração](docs/BACKUP.md), [Google Cloud](docs/GOOGLE_DRIVE.md) e [validação e limites](docs/VALIDACAO.md).

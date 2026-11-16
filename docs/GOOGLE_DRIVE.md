# Google Drive — configuração e validação V005

**Validação real informada pelo proprietário:** login, upload, listagem e restauração Android → Windows realizados com sucesso. Windows → Android permanece pendente de teste manual. Os testes automatizados usam HTTP simulado, tokens sintéticos e bancos descartáveis.

## O que foi implementado

O modo local continua independente de conta ou internet. Na primeira abertura é oferecido conectar ou continuar sem conta; a escolha fica salva. Configurações → Conectar / gerenciar conta Google permite conectar, ver a identificação em cache, atualizar a identificação e desconectar. O login não é proteção por senha do aplicativo.

Windows usa navegador externo, autorização OAuth por código com PKCE S256, `state` aleatório e retorno somente em loopback com porta efêmera. Android usa Google Identity Services **AuthorizationClient**, com consentimento fornecido pelo Google Play Services e retorno de atividade. Não usa WebView, URI OAuth improvisada, cliente Web com segredo ou troca de código de servidor dentro do APK. No Android o SDK gerencia o protocolo nativo; não há PKCE manual inventado. Novos tokens são obtidos pelo SDK para a mesma conta, sem UI em tarefas automáticas. Se houver necessidade de novo consentimento, o envio fica pendente e o usuário reconecta explicitamente.

Solicita-se apenas `drive.file`. Nome e e-mail da conta são obtidos por `Drive about.get`, que aceita esse escopo; não foram acrescentados escopos de Gmail, acesso completo ao Drive, perfil ou contatos. Tokens e configuração sensível usam `SecureStorage`. O identificador de cliente desktop não é segredo; o segredo desktop opcional é informado localmente e armazenado apenas no armazenamento seguro. A senha da conta Google nunca é solicitada pelo aplicativo.

## 1. Projeto e consentimento Google Cloud

1. Abra [Google Cloud Console](https://console.cloud.google.com/), crie ou escolha um projeto para o GerenciadorAulas. Use **o mesmo projeto** para os clientes Windows e Android que devem compartilhar os backups.
2. Em APIs e serviços → Biblioteca, habilite **Google Drive API**.
3. Em Google Auth Platform, configure Branding (nome GerenciadorAulas e contato), Audience e Data Access. Para conta pessoal, use público externo em teste e inclua suas contas como usuários de teste. Para organização, siga as políticas do administrador.
4. Declare somente `https://www.googleapis.com/auth/drive.file`. Não amplie para `drive` para contornar erro de configuração ou falta de visibilidade.
5. Crie os clientes OAuth das plataformas abaixo. Não crie conta de serviço para acessar o Drive pessoal.
6. Em modo externo de teste, tokens de atualização desktop podem expirar em sete dias, conforme a política OAuth do Google. Publicação externa e eventuais verificações precisam ser avaliadas pelo proprietário antes de distribuir a terceiros.

## 2. Windows

1. Crie cliente OAuth de tipo **Aplicativo para computador**, no projeto acima.
2. Copie o Client ID para Configurações → Conta Google no Windows. Se o Google exigir o `client_secret` do cliente desktop, informe-o no campo protegido. Esse campo não é a senha Google. Não coloque o JSON do cliente nem segredo no repositório.
3. Clique Conectar com Google. O navegador deve abrir o domínio oficial Google. Autorize somente o acesso aos arquivos do aplicativo. O listener local aguarda três minutos; cancelamento ou timeout não removem os dados locais.
4. Confira a conta exibida. O aplicativo prepara `GerenciadorAulas/Backups`, sem enviar registros educacionais nessa etapa.
5. Feche e reabra: a sessão segura deve ser reaproveitada, sem pedir login a cada abertura. Reconectar permanece disponível se a permissão expirar ou for revogada.

## 3. Android

Requer dispositivo compatível com Android 8+ e Google Play Services funcionando. Este fluxo não depende de um Client ID Web nem de servidor com segredo.

1. No mesmo projeto Google Cloud, crie cliente OAuth de tipo **Android**.
2. Pacote: `br.com.gerenciadoraulas.app`.
3. Cadastre o SHA-1 do certificado que realmente assinou o APK instalado. O APK Debug desta tarefa usa chave de desenvolvimento; distribuição e Play App Signing exigem o certificado correspondente e podem precisar de outro cliente Android.
4. Para obter o fingerprint público do APK, use a ferramenta oficial já disponível:

   ```powershell
   & "$env:LOCALAPPDATA\GA003\android\build-tools\36.0.0\apksigner.bat" verify --print-certs artifacts/apk/GerenciadorAulas-V005-Debug.apk
   ```

   Se necessário, defina `JAVA_HOME` para a pasta JDK 21 instalada em `%LOCALAPPDATA%\GA003\jdk21`. Copie a linha **Signer #1 certificate SHA-1 digest**, não uma chave privada. Não envie o arquivo de keystore ao GitHub.
5. Instale o APK, abra Configurações → Conta Google e clique Conectar. A tela de conta/consentimento deve ser do Google Play Services. A correspondência pacote/certificado/projeto é usada pelo SDK; não há campo de segredo no Android.
6. Confira o e-mail exibido e a criação das pastas. Teste cancelamento, falta de internet, revogação e nova conexão. APKs com assinatura diferente podem não atualizar a instalação existente; preserve dados e backups antes de qualquer troca de distribuição.

## 4. Backup e restauração entre dispositivos

- Conectar prepara as pastas e identifica a conta, sem upload educacional. Em Backup Google Drive, informe uma **senha de recuperação de pelo menos 12 caracteres**, diferente da senha Google. Ela criptografa os backups AES-GCM e não é enviada ao Google nem incluída no backup. Fica somente em memória na sessão atual.
- Fazer backup agora no Drive é uma autorização explícita de envio. Cada envio cria uma versão datada; não há exclusão automática de versões antigas. O aplicativo só registra sucesso após conferir tamanho e MD5 do Drive; o SHA-256 do arquivo cifrado também acompanha a versão.
- Backups grandes usam sessão de upload em blocos de 1 MiB, com confirmação de progresso e uma consulta de recuperação após falha de transporte. Uma sessão não concluída não é marcada como sucesso. Retomada de sessão entre reinicializações do aplicativo não está implementada; a próxima tentativa cria uma nova versão.
- Backup automático é opcional, com consentimento explícito e intervalo mínimo configurável. Aguarda 30 segundos sem mudanças (com limite de espera de cinco minutos), respeita o intervalo mínimo, evita reenviar a mesma revisão e tenta novamente após falha. Funciona **somente enquanto o aplicativo estiver aberto**. Após reabrir, a senha de recuperação precisa ser informada novamente; não é armazenada em disco. Não há serviço Android em segundo plano nesta versão.
- No segundo dispositivo, conecte a mesma conta e use Listar backups. O mais recente é marcado; cada item mostra data, dispositivo, tamanho e esquema. O escopo `drive.file` vê somente arquivos criados ou autorizados para o mesmo aplicativo/projeto; arquivos de outro projeto não aparecem automaticamente. Google Picker para conceder acesso a arquivos externos não está implementado.
- Ao escolher um backup, informe sua senha de recuperação. Download incompleto ou checksum divergente é recusado. A autenticação AES-GCM, SHA-256 dos arquivos, esquema SQLite e vínculos são verificados antes de substituir dados.
- A restauração exige confirmação, avisa sobre alterações locais mais recentes e cria backup de segurança criptografado do estado local antes da troca. Não há mesclagem. Após restaurar, o backup automático é desativado até nova autorização.
- Desconectar remove tokens e vínculos locais mesmo sem internet. Se não for possível confirmar revogação remota, o aplicativo informa que ela deve ser feita em [Permissões da conta Google](https://myaccount.google.com/permissions). Revogar o acesso do aplicativo pode exigir reconexão em outros dispositivos.
- Limites existentes: até 256 MiB de conteúdo no backup e 50 MiB por evidência. A memória disponível no celular também pode limitar backups grandes, pois criptografia/descriptografia ainda usam buffers em memória.

## 5. Validação real pendente

O proprietário confirmou Android → Windows. Falta confirmar Windows → Android com dados fictícios, além de renovação sem interação, cancelamento, internet indisponível, upload interrompido, senha incorreta, recuperação de backup antigo, cópia de segurança e persistência após reabrir nos dispositivos reais. Os testes simulados não certificam a configuração do Google Cloud nem a integração real dos dois clientes.

## Referências oficiais consultadas

- [OAuth para aplicativos desktop, PKCE e loopback](https://developers.google.com/identity/protocols/oauth2/native-app).
- [Autorização nativa Android](https://developer.android.com/identity/authorization).
- [AuthorizationClient](https://developers.google.com/android/reference/com/google/android/gms/auth/api/identity/AuthorizationClient) e [AuthorizationRequest.Builder](https://developers.google.com/android/reference/com/google/android/gms/auth/api/identity/AuthorizationRequest.Builder).
- [Escopos da Drive API](https://developers.google.com/workspace/drive/api/guides/api-specific-auth) e [about.get](https://developers.google.com/workspace/drive/api/reference/rest/v3/about/get).
- [Uploads e sessões retomáveis](https://developers.google.com/workspace/drive/api/guides/manage-uploads).

Os clientes foram configurados pelo proprietário. Credenciais permanecem fora do repositório. A confirmação manual cobre Android → Windows; não comprova o sentido Windows → Android.

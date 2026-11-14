# Configuração Google Drive

Não foram fornecidas credenciais OAuth. Nenhum login ou envio real foi realizado. Os testes usam tokens sintéticos e HTTP simulado. O modo local não exige conta Google.

## Windows (fluxo implementado, validação real pendente)

1. No Google Cloud Console, crie/selecione um projeto e habilite **Google Drive API**.
2. Configure Google Auth Platform / tela de consentimento: nome GerenciadorAulas, contato de suporte e público adequado. Em modo de teste, adicione explicitamente as contas de teste. Tokens de atualização de aplicativos externos em teste podem expirar; verifique as regras atuais do Google antes de disponibilizar a outras famílias.
3. Solicite somente `https://www.googleapis.com/auth/drive.file`. Não solicite acesso a todos os documentos, Gmail ou senha da conta.
4. Crie um cliente OAuth de tipo **Aplicativo para computador**. No aplicativo Windows, preencha o Client ID. Se o documento de configuração fornecido pelo Google exigir `client_secret` para o cliente desktop, informe-o no campo protegido; ele não é embutido no executável ou no Git. Um segredo de cliente desktop não torna um aplicativo instalado um cliente confidencial.
5. Clique **Conectar conta Google (Windows)**. O navegador padrão abre o endpoint oficial com PKCE S256 e state aleatório. O aplicativo aguarda somente em `http://127.0.0.1:PORTA/`, com porta efêmera e limite de três minutos. Clientes de desktop suportam esse retorno loopback; não configure cliente Web nem reutilize o callback no Android.
6. Tokens e parâmetros necessários à atualização são salvos pelo `SecureStorage` da plataforma. **Desconectar** revoga o token no Google e remove os vínculos locais após resposta aceita. Sem rede, a revogação deve ser repetida quando houver conexão.
7. Defina uma senha de recuperação e clique **Fazer backup agora no Drive**. Confirme a pasta visível `GerenciadorAulas/Backups`, data do envio e listagem no aplicativo.
8. Em outro dispositivo compatível, conecte a mesma conta no mesmo projeto Google Cloud, liste, selecione e restaure em um banco de teste. Confira alunos, sessões e anexos. Teste também sem rede e com senha incorreta.

O escopo `drive.file` limita a visibilidade aos arquivos que o aplicativo criou ou aos quais recebeu acesso. Se backups/pastas não aparecerem entre clientes, confira se pertencem ao mesmo aplicativo/projeto e se a conta concedeu acesso; não amplie silenciosamente para `drive`. O seletor Google Picker para conceder acesso a uma pasta preexistente não foi implementado.

O protocolo implementado cria/localiza as pastas, guarda o ID em armazenamento protegido, envia versões datadas, valida tamanho/MD5 fornecidos pelo Drive e lista com paginação. MD5 é usado somente para verificar a transferência conforme a API; autenticação AES-GCM e SHA-256 protegem o conteúdo do backup. Pasta removida externamente pode exigir desconectar e reconectar para limpar o ID salvo.

## Android (pendência de implementação)

O Google restringe o uso de esquemas URI personalizados e de loopback em clientes Android. O `WebAuthenticator` do MAUI não resolve por si só essa restrição. Por isso o botão de conexão informa que o fluxo Android não está disponível, em vez de enviar credenciais por um caminho incorreto.

Para concluir a integração:

1. Crie cliente **Android** para pacote `br.com.gerenciadoraulas.app` e SHA-1 do certificado de assinatura real. Debug e distribuição podem precisar de clientes separados.
2. Integre a autorização nativa Google Identity Services/AuthorizationClient em atividade Android, com navegador/tela autorizada do Google e permissão `drive.file`. Reavalie o requisito de PKCE explícito conforme a API nativa: ele não deve ser inventado por parâmetros incompatíveis. Se for obrigatório um fluxo de navegador PKCE controlado pelo aplicativo, defina com o proprietário uma arquitetura de callback oficialmente suportada antes de implementá-la.
3. Trate resultado da atividade, consentimento, cancelamento, expiração e renovação; adapte a abstração de autorização ao `IBackupDrive`. Não use uma conta de serviço nem segredo de cliente Web dentro do APK.
4. Valide em aparelho físico com a conta de teste, incluindo troca de aparelho, revogação e restauração. Hoje o Android utiliza backup local completo.

Referências oficiais para conferência pelo proprietário: [OAuth para aplicativos instalados](https://developers.google.com/identity/protocols/oauth2/native-app), [autorização Android](https://developers.google.com/identity/authorization/android), [escopos Drive](https://developers.google.com/workspace/drive/api/guides/api-specific-auth). A documentação online do Google não pôde ser consultada neste ambiente (HTTP 403); o fluxo Android permanece explicitamente pendente.

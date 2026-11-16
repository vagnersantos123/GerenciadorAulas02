# Backup e restauração

Em **Backup Google Drive**, defina uma senha de recuperação de pelo menos 12 caracteres. Guarde-a em local seguro; não há recuperação de senha. A senha fica somente em memória durante a sessão do aplicativo, não em SQLite, manifesto ou Git.

**Exportar backup local** cria `.gabackup` e abre o compartilhamento do sistema para você salvar o arquivo. Confirme que o arquivo foi efetivamente salvo no destino escolhido. O aplicativo não assume que concluir a exportação equivale a copiar para o Drive.

Formato versão 1: cabeçalho `GABK0001`, sal aleatório de 16 bytes, nonce de 12 bytes, tag de 16 bytes e ZIP cifrado com AES-256-GCM. Chave derivada por PBKDF2-HMAC-SHA256 (600.000 iterações); cabeçalho autenticado como AAD. O ZIP contém cópia consistente obtida pela API SQLite Backup, anexos referenciados e manifesto com esquema, dispositivo, revisão, UTC, tamanhos e SHA-256. Tokens Google e preferências de autenticação não entram no pacote. O dispositivo fica no manifesto criptografado e, no Drive, também em metadados visíveis da conta.

Limites: 50 MB por anexo, 256 MB de conteúdo descompactado e até 10.000 entradas no backup. Criptografia desta primeira versão usa memória; acervos maiores precisam de uma implementação por fluxo em versão futura. Não inclua dados reais em testes.

Para restaurar:

1. Selecione **Restaurar arquivo local** ou um item listado no Drive.
2. Informe a senha correta. O aplicativo autentica e verifica o pacote, caminhos, duplicações, hashes, SQLite e vínculos antes de substituir dados.
3. Confirme a substituição integral. Se houver mudanças locais posteriores à criação do backup, há uma confirmação adicional, inclusive para exclusões locais.
4. O estado local é exportado para um arquivo `seguranca-*.gabackup`, criptografado com a senha informada, no diretório privado pai de `journal-v1`.
5. O banco e os anexos são trocados juntos mediante área de preparação e proteção contra alterações concorrentes. Não há mesclagem entre dispositivos.

A cópia de segurança anterior fica no armazenamento privado e a mensagem final informa seu nome. Para recuperá-la sem acesso direto ao diretório privado, use **Exportar cópia de segurança anterior** na tela de backup. Guarde-a externamente se precisar preservá-la após desinstalar. O banco de uso diário não é cifrado por este formato: depende da proteção do armazenamento privado do sistema. Arquivos abertos em outro aplicativo podem gerar cópias no cache; não abra documentos sensíveis em aplicativos não confiáveis.

O backup automático do Drive funciona somente com o aplicativo aberto, conta conectada e senha preenchida na sessão. Aguarda 30 segundos após alterações, verifica tamanho/checksum do upload e só então registra sucesso. Falhas mantêm o estado pendente e tentam novamente após espera. Não há serviço Android em segundo plano nem agendamento com o aplicativo encerrado. A autorização do modo automático é persistida; a senha de recuperação precisa ser informada novamente a cada abertura. Não há expurgo automático de versões no Drive.

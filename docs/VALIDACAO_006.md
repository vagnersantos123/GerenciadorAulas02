# V006 — confiabilidade e preparação para uso diário

Esta etapa é preparada para publicação na branch `feature/v006-preparacao-v1`, baseada na V005 (`34cf774`), sem alterar a `main`. Nenhum teste desta etapa acessa bancos pessoais; as novas bases são criadas em diretórios temporários exclusivos.

V005 publicada por push normal `9c4a7ef..34cf774`. `ls-remote` confirmou `refs/heads/main` em `34cf77492fa86fef1c2bc665cae0d418f7bf5593`. O stash continua em `8dd418bfeb62f7c6d9859c40f42b0e2a84d5f2a2`. Essa confirmação não representa aprovação dos jobs remotos de CI.

## Evidência real e limites

O proprietário confirmou login Google, upload, listagem e restauração **Android → Windows**. **Windows → Android permanece pendente**. Os testes automatizados não substituem essa confirmação no celular, nem comprovam comportamento sob encerramento forçado, bateria, rotação ou falta de espaço real.

## Resultado automatizado

- V005 antes da correção local: **79/79 testes aprovados**.
- V006: **91/91 aprovados**, zero falhas/ignorados, incluindo verificação de espera e nova tentativa após timeout. A reprodução anterior registrou a falha esperada de timeout antes da correção (`artifacts/consolidation-005/v006-before-fix.log`). Resultado final: `artifacts/validation-006/tests-final.log`.
- Aviso já conhecido do executor: `VSTEST_ARTIFACTS_DIRECTORY` ausente. Não afetou os resultados e não motivou atualização de pacotes.
- O banco e o resultado de validação V004 conferidos contra o baseline mantiveram seus SHA-256. Nenhuma base pessoal foi aberta.
- Windows V006 compilado sem MSIX: **zero erros e zero avisos**, em cópia separada (`artifacts/validation-006/windows.log`). A consulta NuGet Audit foi desativada nessa compilação de validação; isso não equivale a uma auditoria de vulnerabilidades dos pacotes.
- Android V006: **zero erros e um aviso XA0033**, em 5min59s (`artifacts/validation-006/android.log`). Não houve instalação do APK nem teste adicional em dispositivo nesta etapa.
- Houve conflito de arquivos intermediários ao executar as primeiras compilações Windows/Android em paralelo no mesmo projeto. A repetição Android é sequencial; a compilação Windows V006 usa uma cópia separada. Não foi feita alteração funcional para contornar essa falha de execução.

## APK V006 e alterações locais

- APK: `artifacts/apk/GerenciadorAulas-V006-Debug.apk`, **89.680.554 bytes**.
- SHA-256: `D23AAE5604715E616EB7B5429A207D68A6B704EAB5043965B11251C1AEFCB2F5`.
- `apksigner verify --verbose --print-certs`: código 0, assinaturas v2 e v3 válidas. Mesmo certificado Debug V005: SHA-1 `8C:01:CC:66:3A:B7:78:32:25:E8:22:AC:63:F9:EB:61:49:B1:FD:A3`.
- Pacote `br.com.gerenciadoraulas.app`, versionCode 1, versão 1.0.0, alvo API 36, ARM64/x86_64. Não é uma distribuição Release nem estabelece política de versionamento de produção.
- Arquivos que compõem esta entrega V006: `src/GerenciadorAulas.Core/GoogleDrive.cs` (timeout automático), `tests/GerenciadorAulas.Tests/Reliability006Tests.cs` (12 casos), `docs/BACKUP.md` (persistência da autorização automática) e este relatório.
- O APK V005 anterior e o APK da revisão foram preservados. Nenhum SDK, workload, pacote ou componente da IDE foi instalado/atualizado nesta etapa.

## Casos automatizados da V006

`Reliability006Tests.cs` verifica restauração em outro banco, igualdade dos IDs e conteúdo de alunos, anos, matérias, lições, planejamentos, sessões com tempos e anexos, inclusive após reabrir. A cópia de segurança do destino também é restaurada em um terceiro banco descartável.

Os casos negativos cobrem senha incorreta, pacote truncado, alteração no cabeçalho/cifra, manifesto com esquema futuro, SHA-256 incorreto e anexo ausente. A comparação do SHA-256 do banco de destino exige que permaneça byte a byte intacto após a recusa; diretórios temporários de restauração devem ser removidos. Um anexo alterado na origem impede criar um backup válido. Estudo sem conta salva, reabre e não conclui lições automaticamente.

Foi reproduzida uma falha não destrutiva: timeout de transporte (`TaskCanceledException` sem cancelamento solicitado pelo chamador) escapava de `TickAsync`, sem aplicar a espera de nova tentativa. Nenhum sucesso era registrado e o timer da interface já capturava a exceção, mas faltava tratar o timeout no coordenador. A V006 trata esse caso como envio pendente, mantendo o cancelamento explícito do chamador propagado.

Testes V005 já cobrem download truncado/checksum incorreto, conexão indisponível, 401, escopo negado, PKCE/state, renovação, retomada de progresso reconhecido pelo servidor e rejeição de URL de upload externa. Não há retomada de sessão entre reinicializações.

## Obrigatório antes do uso real

- Concluir o roteiro Windows → Android abaixo e conferir a cópia de segurança anterior. Não aprovar os dois sentidos apenas porque um deles funcionou.
- Testar atualização sobre instalação existente em Windows e Android, primeiro com banco fictício e backup externo verificado; conferir persistência após fechar/reabrir e funcionamento offline.
- Definir distribuição Android Release, assinatura durável e custódia da chave fora do Git, com cópia segura. O APK atual é Debug. Cadastrar no OAuth o certificado efetivamente usado na distribuição; não trocar assinatura sobre dados sem plano de recuperação.
- Definir incremento de versão de distribuição (`ApplicationVersion`/versionCode e versão exibida) antes de entregar novas versões. Hoje as versões de validação usam 1/1.0.0, não uma política de lançamento concluída.
- Conservar backup externo recente e testar sua recuperação. Guardar a senha de recuperação fora do aplicativo. O Drive e a cópia no diretório privado não substituem essa precaução; desinstalar pode apagar a cópia privada.
- Validar falta de espaço, interrupção do processo durante a troca de diretórios e arquivos bloqueados em dispositivo de teste. A troca protege falhas usuais, mas não foi certificada como recuperação automática de queda de energia em qualquer ponto. Diretórios `.rollback-*` não devem ser apagados manualmente sem diagnóstico.
- Manter o formato `GABK0001` e o esquema SQLite 1 compatíveis. Esquema futuro é recusado. Qualquer mudança futura exige migração versionada e testes com backups anteriores; não baixar a versão sobre banco migrado sem procedimento de recuperação.
- Conferir cancelamento/revogação e renovação de autorização nos dois dispositivos. Modo OAuth externo em teste pode exigir nova autorização; confirmar configuração de produção antes da distribuição a terceiros.
- Ensaiar navegação, toque, seleção extensa de lições, registro e anexos em aparelhos reais. Não há medição de desempenho ou certificação completa da interface nesta revisão.

## Atualização segura

1. Antes de atualizar, criar backup cifrado, conferir listagem e guardar cópia externa com a senha. Testar a recuperação em ambiente fictício separado.
2. Encerrar o aplicativo nos dois dispositivos. Preservar bancos, anexos, backups e instaladores anteriores. Não usar limpar dados/desinstalar como solução para erro de atualização.
3. Windows: manter identidade/configuração de armazenamento e substituir somente a pasta de binários, conservando os dados privados. Distribuir a pasta completa; não apenas o executável. Nunca apontar versão de teste para banco pessoal.
4. Android: verificar pacote, certificado e versionCode antes da instalação; instalar como atualização compatível. Se houver assinatura incompatível, interromper e preparar migração via backup validado, sem desinstalar por tentativa.
5. Reabrir e conferir alunos, matérias, calendário, planejamento, tempos e anexos; registrar um estudo fictício e reabrir. Conferir offline antes de reativar o backup automático.
6. Em caso de incompatibilidade, preservar a base ativa e diagnosticar em cópia. Restauração substitui integralmente; não há mesclagem nem sincronização simultânea.

## Roteiro manual pendente Windows → Android

1. Usar somente a janela Windows de banco fictício e um Android/perfil sem dados pessoais. Não reutilizar perfil com registros pessoais.
2. Registrar identificadores e contagens de alunos, anos, matérias, lições, planos, sessões e anexos. Incluir sessão atravessando meia-noite e uma evidência fictícia conhecida.
3. Conectar a mesma conta/projeto Google nas duas plataformas. Manter backup automático desligado durante o ensaio.
4. Criar backup manual Windows com senha de teste de ao menos 12 caracteres; anotar nome/ID, UTC, dispositivo, tamanho e SHA-256. Confirmar envio verificado e listagem.
5. Android: listar e localizar o mesmo arquivo. Primeiro tentar senha incorreta e conferir que o destino não mudou; depois usar a correta e revisar a confirmação de substituição e aviso sobre dados mais novos.
6. Restaurar, conferir todas as contagens, IDs, datas originais do planejamento, estimativas, minutos realizados e bytes/conteúdo do anexo. Conferir a cópia de segurança criada antes da troca.
7. Encerrar/reabrir, ativar modo avião manualmente e conferir leitura e novo estudo fictício. Verificar que não foi exigido login para uso local. Reabrir novamente para confirmar persistência.
8. Registrar versão instalada, modelo/Android, resultado e erros. Somente então marcar Windows → Android como aprovado.

## Melhorias posteriores

Alarmes e notificações, manual com imagens, backup em segundo plano, retenção de versões, retomada persistente de upload e criptografia em fluxo para acervos grandes continuam futuras. Não são implementadas nesta etapa. Revisar consumo de memória antes de ampliar os limites atuais de backup/anexos.

## Avaliação V1.0

Adequado para continuar validações controladas com dados fictícios. A liberação para uso diário depende dos itens obrigatórios, especialmente recuperação inversa, atualização preservando dados, assinatura de distribuição e ensaios de falha no dispositivo. Testes automatizados aprovados, isoladamente, não certificam ausência de perda sob qualquer falha do sistema.

Referências oficiais: [assinatura e atualização Android](https://developer.android.com/studio/publish/app-signing), [OAuth desktop](https://developers.google.com/identity/protocols/oauth2/native-app).

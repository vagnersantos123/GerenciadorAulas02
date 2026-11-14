# Evidências e limites

Validação no Linux em 14/11/2026. SDK 10.0.100, conjunto estável de workloads 10.0.100, JDK Temurin 21.0.9+10, Android API 36/build-tools 36.0.0. Instalações em `/workspace` autorizadas pelo proprietário, sem instalação no sistema operacional. Assinaturas NuGet verificadas explicitamente; verificador retornou sucesso com avisos de indisponibilidade de consulta de revogação. JDK e arquivos Android conferidos contra os checksums oficiais.

## Automatizado

`dotnet test tests/GerenciadorAulas.Tests`: 19 cenários, nenhum ignorado:

- Uma lição em três dias, sem conclusão automática.
- Três lições em uma sessão, gravação repetida e cliques concorrentes sem duplicação.
- Férias, feriados, capacidade de calendário e planejamento original imutável.
- Reforço com várias lições e histórico após superação.
- Reabertura e inicialização repetida do banco.
- Rejeição de vínculos entre matérias e de exclusões com dependentes.
- Exclusão transacional de matéria sem histórico e proteção das lições com histórico.
- Contagem inválida de exercícios/acertos sem gravação parcial.
- Backup completo com anexos deduplicados, restauração e cópia de segurança.
- Backup danificado, senha errada e ausência de confirmação sem substituição local.
- Alterações e exclusões posteriores ao backup exigem confirmação adicional.
- Falha de Drive não registra envio bem-sucedido.
- HTTP simulado valida criação de pastas, upload/listagem/download e rejeição de checksum incorreto.
- OAuth recusa state divergente antes de qualquer solicitação de token.
- Relatório escapa HTML e oculta ocorrências sensíveis por padrão.

As simulações não provam login, consentimento nem comunicação real com o Google.

## Compilação

- Android Debug compilou sem avisos ou erros e produziu APK assinado de desenvolvimento.
- Windows: restauração e compilação do núcleo chegaram ao compilador WinUI. O host Linux recusou executar `XamlCompiler.exe` com `Exec format error` / MSB3073, código 126. Nenhum teste de inicialização Windows foi realizado; não se afirma que o executável Windows foi gerado.
- Os comandos de compilação Windows isolam esse alvo com `BuildWindowsOnly=true`, para não restaurar runtime Android com RID Windows.

## Testes manuais ainda necessários

Em Windows e em aparelho Android, cadastrar dois alunos e dois anos, fechar/reabrir, editar e excluir com confirmação, selecionar PDFs e fotografias pelo seletor do sistema, abrir anexos, conferir teclado/data/navegação e layout, gerar relatório e imprimir. Validar todas as áreas com TalkBack/Narrador e telas pequenas. Instalar o APK gerado em aparelho antes de uso com dados importantes.

No Windows, testar navegador/callback OAuth, SecureStorage no aplicativo não empacotado, renovação/revogação e upload/restauração reais. No Android, implementar primeiro a autorização nativa; não está disponível nesta entrega.

## Limites conhecidos

- Windows e fluxos visuais não foram validados em máquinas/dispositivos compatíveis.
- Login Android pendente de implementação; credenciais reais e teste de login Windows pendentes do proprietário.
- Backup automático somente com app aberto e senha disponível em memória. A opção não persiste após encerrar.
- Limites de 50 MB por anexo e 256 MB por backup; sem compactação/criptografia por fluxo para acervos grandes.
- Relatórios em HTML imprimível; PDF direto e incorporação de documentos não implementados.
- Planejamento exibe inicialmente até 500 itens; lições exibem até 200 resultados com filtro. Não há remanejamento automático, nem mesclagem de dados.
- Sem importação do banco antigo, notificações em segundo plano, retenção automática de versões do Drive ou coleta de anexos órfãos.
- Formulários de pausas e seleção de várias lições usam texto estruturado nesta primeira interface; não há editor visual de calendário.
- Não há certificação jurídica dos registros. Armazenamento ativo é privado, mas não cifrado no nível do banco; o backup exportado é autenticado e cifrado.

Esses limites devem ser considerados antes de adotar a versão como único registro familiar. Preserve cópias dos dados antigos e teste restaurações com dados fictícios primeiro.

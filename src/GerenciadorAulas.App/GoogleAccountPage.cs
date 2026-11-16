using GerenciadorAulas.Core;
namespace GerenciadorAulas.App;
public sealed class GoogleAccountPage : ContentPage
{
    private readonly IServiceProvider services;
    private readonly WorkspaceViewModel vm = new();
    private readonly VerticalStackLayout fields = new() { Padding = 24, Spacing = 12, MaximumWidthRequest = 720, HorizontalOptions = LayoutOptions.Fill };
    private readonly Func<Task>? connected;
    public GoogleAccountPage(IServiceProvider services, Func<Task>? connected = null)
    {
        this.services = services; this.connected = connected; Title = "Conta Google (opcional)"; BindingContext = vm; Content = new ScrollView { Content = fields };
    }
    protected override async void OnAppearing() { base.OnAppearing(); await vm.Command(LoadAsync).ExecuteAsync(null); }
    private void Note(string text) => fields.Add(new Label { Text = text, FontSize = 16 });
    private Button Action(string text, Func<Task> action) => new() { Text = text, Command = vm.Command(action) };
    private async Task LoadAsync()
    {
        fields.Clear(); var auth = services.GetRequiredService<GoogleAuthorization>(); var drive = services.GetRequiredService<GoogleDriveClient>();
        var account = await auth.AccountAsync(); var hasSession = await auth.HasSessionAsync();
        Note("O GerenciadorAulas funciona sem conta e sem internet. Conectar autoriza acesso aos backups do aplicativo no Drive; não protege a abertura do programa por senha.");
        Note(account is null ? hasSession ? "Autorização salva; identificação da conta pendente. Atualize a identificação com internet." : "Nenhuma conta Google conectada." : $"Conta conectada: {account.DisplayName}\n{account.Email}");
        Note("Conectar não envia registros educacionais. O envio só ocorre pelo botão de backup ou ao autorizar o backup automático. A senha Google é informada exclusivamente ao Google.");
#if WINDOWS
        Note("Configure o cliente OAuth para computador no Google Cloud. Client ID identifica o aplicativo; não é uma senha. O campo abaixo é o segredo de configuração desktop, nunca a senha da conta.");
        var id = new Microsoft.Maui.Controls.Entry { Placeholder = "Client ID para computador", Text = Preferences.Default.Get("google-client-id", "") }; fields.Add(id);
        var secret = new Microsoft.Maui.Controls.Entry { Placeholder = "Client secret desktop, se exigido", IsPassword = true }; fields.Add(secret);
#else
        Note("Android: autorização nativa Google Play Services. O proprietário precisa cadastrar o pacote e o certificado no Google Cloud. Nenhum segredo de cliente Web é usado no celular.");
#endif
        fields.Add(Action(hasSession ? "Reconectar com Google" : "Conectar com Google", async () =>
        {
            var coordinator = services.GetRequiredService<BackupCoordinator>(); await coordinator.ConfigureAsync(false);
#if WINDOWS
            await DesktopGoogleLogin.ConnectAsync(auth, id.Text ?? "", secret.Text); secret.Text = ""; Preferences.Default.Set("google-client-id", id.Text ?? "");
#else
            await auth.ConnectNativeAsync();
#endif
            await drive.GetAccountAsync(); await drive.PrepareFoldersAsync(); await services.GetRequiredService<GoogleOnboarding>().CompleteAsync();
            await DisplayAlertAsync("Conta autorizada", "As pastas GerenciadorAulas/Backups estão prontas. Nenhum registro educacional foi enviado. Configure o backup na seção Backup Google Drive.", "OK");
            if (connected is not null) await connected(); else await LoadAsync();
        }));
        if (hasSession)
        {
            fields.Add(Action("Atualizar identificação da conta", async () => { await drive.GetAccountAsync(); await LoadAsync(); }));
            fields.Add(Action("Desconectar conta Google", async () =>
            {
                if (!await DisplayAlertAsync("Desconectar?", "Os dados locais serão mantidos. O backup automático será desativado neste dispositivo.", "Desconectar", "Cancelar")) return;
                await services.GetRequiredService<BackupCoordinator>().ConfigureAsync(false);
                var revoked = await auth.DisconnectAsync();
                await DisplayAlertAsync("Conta desconectada", revoked ? "Credenciais locais removidas e autorização revogada no Google." : "Credenciais locais removidas. Não foi possível confirmar a revogação remota; remova o acesso em myaccount.google.com/permissions quando estiver online.", "OK");
                await LoadAsync();
            }));
        }
        var error = new Label { TextColor = Color.FromArgb("#9C1C1C") }; error.SetBinding(Label.TextProperty, nameof(vm.Error)); fields.Add(error);
    }
}
public sealed class GoogleWelcomePage : ContentPage
{
    public GoogleWelcomePage(IServiceProvider services)
    {
        Title = "Bem-vindo ao GerenciadorAulas"; var vm = new WorkspaceViewModel(); BindingContext = vm;
        var error = new Label { TextColor = Colors.DarkRed }; error.SetBinding(Label.TextProperty, nameof(vm.Error));
        Content = new ScrollView { Content = new VerticalStackLayout { Padding = 24, Spacing = 20, MaximumWidthRequest = 640, HorizontalOptions = LayoutOptions.Fill, Children = {
            new Label { Text = "Seus estudos ficam neste dispositivo", FontSize = 26, FontAttributes = FontAttributes.Bold },
            new Label { Text = "Use o programa offline ou conecte o Google para preparar backup e restauração entre dispositivos. Conectar não envia seus registros automaticamente. A conta pode ser configurada depois em Configurações." },
            new Button { Text = "Conectar com Google", Command = vm.Command(async () => await Navigation.PushAsync(new GoogleAccountPage(services, async () => await Navigation.PopModalAsync()))) },
            new Button { Text = "Continuar sem conta", Command = vm.Command(async () => { await services.GetRequiredService<GoogleOnboarding>().CompleteAsync(); await Navigation.PopModalAsync(); }) }, error
        } } };
    }
}

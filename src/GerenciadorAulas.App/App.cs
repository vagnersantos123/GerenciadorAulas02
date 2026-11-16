namespace GerenciadorAulas.App;
public class App(IServiceProvider services) : Application
{
    protected override Window CreateWindow(IActivationState? activationState)
    {
        Resources.Add(new Style(typeof(ContentPage)) { Setters = { new Setter { Property = VisualElement.BackgroundColorProperty, Value = Color.FromArgb("#F5F7F5") } } });
        // Keep the green theme while avoiding platform dark text defaults on light cards.
        Resources.Add(new Style(typeof(Label)) { Setters = { new Setter { Property = Label.TextColorProperty, Value = Color.FromArgb("#203B34") }, new Setter { Property = Label.FontSizeProperty, Value = 16d } } });
        Resources.Add(new Style(typeof(Microsoft.Maui.Controls.Entry)) { Setters = { new Setter { Property = Microsoft.Maui.Controls.Entry.TextColorProperty, Value = Color.FromArgb("#203B34") }, new Setter { Property = Microsoft.Maui.Controls.Entry.PlaceholderColorProperty, Value = Color.FromArgb("#52645F") }, new Setter { Property = VisualElement.BackgroundColorProperty, Value = Colors.White }, new Setter { Property = VisualElement.MinimumHeightRequestProperty, Value = 48d } } });
        Resources.Add(new Style(typeof(Editor)) { Setters = { new Setter { Property = Editor.TextColorProperty, Value = Color.FromArgb("#203B34") }, new Setter { Property = VisualElement.BackgroundColorProperty, Value = Colors.White } } });
        Resources.Add(new Style(typeof(Picker)) { Setters = { new Setter { Property = Picker.TextColorProperty, Value = Color.FromArgb("#203B34") }, new Setter { Property = Picker.TitleColorProperty, Value = Color.FromArgb("#52645F") }, new Setter { Property = VisualElement.BackgroundColorProperty, Value = Colors.White }, new Setter { Property = VisualElement.MinimumHeightRequestProperty, Value = 48d } } });
        Resources.Add(new Style(typeof(DatePicker)) { Setters = { new Setter { Property = DatePicker.TextColorProperty, Value = Color.FromArgb("#203B34") }, new Setter { Property = VisualElement.BackgroundColorProperty, Value = Colors.White } } });
        Resources.Add(new Style(typeof(TimePicker)) { Setters = { new Setter { Property = TimePicker.TextColorProperty, Value = Color.FromArgb("#203B34") }, new Setter { Property = VisualElement.BackgroundColorProperty, Value = Colors.White } } });
        Resources.Add(new Style(typeof(SearchBar)) { Setters = { new Setter { Property = SearchBar.TextColorProperty, Value = Color.FromArgb("#203B34") }, new Setter { Property = SearchBar.PlaceholderColorProperty, Value = Color.FromArgb("#52645F") }, new Setter { Property = VisualElement.BackgroundColorProperty, Value = Colors.White } } });
        Resources.Add(new Style(typeof(Button)) { Setters = { new Setter { Property = Button.BackgroundColorProperty, Value = Color.FromArgb("#14594E") }, new Setter { Property = Button.TextColorProperty, Value = Colors.White }, new Setter { Property = Button.CornerRadiusProperty, Value = 12 }, new Setter { Property = VisualElement.MinimumHeightRequestProperty, Value = 48d } } });
        var shell = new Shell { Title = "GerenciadorAulas", FlyoutBehavior = FlyoutBehavior.Flyout };
        foreach (var section in new[] { "Início", "Alunos", "Anos letivos", "Matérias e lições", "Planejamento", "Registrar estudo", "Histórico", "Dificuldades e reforços", "Atividades e avaliações", "Ocorrências", "Evidências", "Relatórios", "Configurações", "Backup Google Drive" })
            shell.Items.Add(new FlyoutItem { Title = section, Items = { new ShellContent { Title = section, ContentTemplate = new DataTemplate(() => new WorkspacePage(services, section)) } } });
        var window = new Window(shell) { Title = "GerenciadorAulas", Width = 1100, Height = 800 };
        var coordinator=services.GetRequiredService<GerenciadorAulas.Core.BackupCoordinator>();
        var store=services.GetRequiredService<GerenciadorAulas.Core.JournalStore>();
        var context=services.GetRequiredService<SelectionContext>();
        EventHandler changed=(_,_)=>coordinator.NotifyChange();store.Changed+=changed;
        var timer=Dispatcher.CreateTimer();timer.Interval=TimeSpan.FromSeconds(5);bool ticking=false;
        timer.Tick+=async(_,_)=>
        {
            if(ticking)return;ticking=true;
            try { await coordinator.TickAsync(context.RecoveryPassword,DeviceInfo.Name); }
            catch(Exception ex) { System.Diagnostics.Debug.WriteLine("Backup automático: "+ex.GetType().Name); }
            finally { ticking=false; }
        };
        window.Created+=(_,_)=>timer.Start();window.Destroying+=(_,_)=>{timer.Stop();store.Changed-=changed;context.RecoveryPassword=null;};
        window.Created+=(_,_)=>Dispatcher.Dispatch(async()=>
        {
            try
            {
                await coordinator.InitializeAsync();
                var onboarding=services.GetRequiredService<GerenciadorAulas.Core.GoogleOnboarding>();
                if(await onboarding.ShouldOfferAsync())await shell.Navigation.PushModalAsync(new NavigationPage(new GoogleWelcomePage(services)));
            }
            catch(Exception ex){System.Diagnostics.Debug.WriteLine("Conta opcional indisponível: "+ex.GetType().Name);}
        });
        return window;
    }
}

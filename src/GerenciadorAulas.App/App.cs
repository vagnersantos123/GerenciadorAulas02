namespace GerenciadorAulas.App;
public class App(IServiceProvider services) : Application
{
    protected override Window CreateWindow(IActivationState? activationState)
    {
        Resources.Add(new Style(typeof(ContentPage)) { Setters = { new Setter { Property = VisualElement.BackgroundColorProperty, Value = Color.FromArgb("#F5F7F5") } } });
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
        return window;
    }
}

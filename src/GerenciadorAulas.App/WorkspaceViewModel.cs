using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
namespace GerenciadorAulas.App;
public sealed class WorkspaceViewModel : ObservableObject
{
    private bool busy;
    private string error = "";
    public bool Busy { get => busy; private set => SetProperty(ref busy,value); }
    public string Error { get => error; private set => SetProperty(ref error,value); }
    public IAsyncRelayCommand Command(Func<Task> action) => new AsyncRelayCommand(async () =>
    {
        if (Busy) return;
        Busy=true;Error="";
        try { await action(); }
        catch (Exception ex) { Error = ex is ArgumentException or InvalidOperationException or IOException or NotSupportedException or HttpRequestException ? ex.Message : "Não foi possível concluir. Seus dados locais foram preservados; tente novamente."; System.Diagnostics.Debug.WriteLine("GerenciadorAulas: " + ex.GetType().Name); }
        finally { Busy=false; }
    });
}

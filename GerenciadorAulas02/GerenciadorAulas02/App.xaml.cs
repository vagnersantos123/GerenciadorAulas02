using Microsoft.Maui.Controls;
using System.IO;
using GerenciadorAulas02.Services;

namespace GerenciadorAulas02;

public partial class App : Application
{
    // ✅ Banco de dados público e estático
    public static AulaDatabase Database { get; private set; }

    public App()
    {
        InitializeComponent();



        // Caminho do arquivo SQLite
        string dbPath = Path.Combine(FileSystem.AppDataDirectory, "GerenciadorAulas.db3");
        Database = new AulaDatabase(dbPath);
        System.Diagnostics.Debug.WriteLine($"DB Path: {dbPath}");

        Application.Current.UserAppTheme = Services.PreferenciasGlobais.TemaEscuro ? AppTheme.Dark : AppTheme.Light;




        // Página inicial
        MainPage = new NavigationPage(new Views.LoginPage());

        if (Preferences.Get("ManterConectado", false))
            MainPage = new NavigationPage(new Views.HomePage());
        else
            MainPage = new NavigationPage(new Views.LoginPage());
    }
}

using GerenciadorAulas.Core;
namespace GerenciadorAulas.App;
public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder().UseMauiApp<App>();
        builder.Services.AddSingleton(new JournalStore(Path.Combine(FileSystem.AppDataDirectory, "journal-v1")));
        builder.Services.AddSingleton<EducationService>();
        builder.Services.AddSingleton<EvidenceService>();
        builder.Services.AddSingleton<BackupService>();
        builder.Services.AddSingleton<SelectionContext>();
        builder.Services.AddSingleton<HttpClient>();
        builder.Services.AddSingleton<ITokenVault, PlatformTokenVault>();
#if ANDROID
        builder.Services.AddSingleton<INativeGoogleAuthorization, AndroidGoogleAuthorization>();
#endif
        builder.Services.AddSingleton<GoogleOnboarding>();
        builder.Services.AddSingleton<GoogleAuthorization>();
        builder.Services.AddSingleton<GoogleDriveClient>();
        builder.Services.AddSingleton<IBackupDrive>(s => s.GetRequiredService<GoogleDriveClient>());
        builder.Services.AddSingleton<BackupCoordinator>();
        builder.Services.AddSingleton<ReportService>();
        return builder.Build();
    }
}

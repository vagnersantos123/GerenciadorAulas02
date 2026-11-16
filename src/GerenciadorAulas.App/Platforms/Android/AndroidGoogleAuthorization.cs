using Android.App;
using Android.Content;
using Android.Gms.Auth.Api.Identity;
using Android.Gms.Common.Apis;
using Android.Gms.Tasks;
using GerenciadorAulas.Core;
using Task = System.Threading.Tasks.Task;
namespace GerenciadorAulas.App;

// Google Play Services owns the consent UI; no URI callback, WebView, or client secret.
public sealed class AndroidGoogleAuthorization : INativeGoogleAuthorization
{
    private const int RequestCode = 4505;
    private static TaskCompletionSource<AuthorizationResult>? pending;
    private readonly SemaphoreSlim gate = new(1, 1);
    private sealed class Listener : Java.Lang.Object, IOnSuccessListener, IOnFailureListener
    {
        public TaskCompletionSource<Java.Lang.Object?> Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public void OnSuccess(Java.Lang.Object? result) => Completion.TrySetResult(result);
        public void OnFailure(Java.Lang.Exception exception) => Completion.TrySetException(new InvalidOperationException("Não foi possível obter autorização Google. Confira rede, Play Services e configuração OAuth do pacote/certificado."));
    }
    private static async System.Threading.Tasks.Task<Java.Lang.Object?> WaitAsync(Android.Gms.Tasks.Task task)
    {
        using var listener = new Listener(); task.AddOnSuccessListener(listener); task.AddOnFailureListener(listener);
        return await listener.Completion.Task.WaitAsync(TimeSpan.FromMinutes(3));
    }
    public async System.Threading.Tasks.Task<OAuthTokens> AuthorizeAsync(bool interactive, string? accountName)
    {
        await gate.WaitAsync();
        try
        {
            return await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                var activity = Platform.CurrentActivity ?? throw new InvalidOperationException("Abra o aplicativo para conectar a conta.");
                var client = Identity.GetAuthorizationClient(activity);
                using var builder = new AuthorizationRequest.Builder(); builder.SetRequestedScopes(new[] { new Scope(GoogleAuthorization.Scope) });
                if (!string.IsNullOrWhiteSpace(accountName)) builder.SetAccount(new Android.Accounts.Account(accountName, "com.google"));
                var result = (AuthorizationResult)(await WaitAsync(client.Authorize(builder.Build())))!;
                if (result.HasResolution)
                {
                    if (!interactive) throw new InvalidOperationException("Reconecte a conta nas Configurações para renovar o consentimento.");
                    pending = new(TaskCreationOptions.RunContinuationsAsynchronously);
                    try
                    {
#pragma warning disable CS0618
                        activity.StartIntentSenderForResult(result.PendingIntent!.IntentSender, RequestCode, null, 0, 0, 0);
#pragma warning restore CS0618
                        result = await pending.Task.WaitAsync(TimeSpan.FromMinutes(3));
                    }
                    finally { pending = null; }
                }
                if (string.IsNullOrWhiteSpace(result.AccessToken) || !result.GrantedScopes.Contains(GoogleAuthorization.Scope)) throw new InvalidOperationException("Permissão drive.file não concedida.");
                return new OAuthTokens(result.AccessToken, null, DateTimeOffset.UtcNow.AddMinutes(45));
            });
        }
        catch (TimeoutException) { throw new InvalidOperationException("Autorização Google expirou. Tente novamente nas Configurações."); }
        finally { gate.Release(); }
    }
    public async Task RevokeAsync(string? accountName)
    {
        if (string.IsNullOrWhiteSpace(accountName)) throw new InvalidOperationException("Conta não identificada para revogação remota.");
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            var activity = Platform.CurrentActivity ?? throw new InvalidOperationException("Abra o aplicativo para desconectar.");
            using var request = RevokeAccessRequest.InvokeBuilder(); request.SetAccount(new Android.Accounts.Account(accountName, "com.google")); request.SetScopes(new[] { new Scope(GoogleAuthorization.Scope) });
            try { await WaitAsync(Identity.GetAuthorizationClient(activity).RevokeAccess(request.Build())); }
            catch (TimeoutException) { throw new InvalidOperationException("Não foi possível revogar no Google agora."); }
        });
    }
    public static bool HandleResult(Android.App.Activity activity, int requestCode, Result resultCode, Intent? data)
    {
        if (requestCode != RequestCode) return false;
        var completion = pending;
        if (completion is null) return true;
        if (resultCode != Result.Ok || data is null) completion.TrySetException(new InvalidOperationException("Conexão Google cancelada."));
        else
        {
            try { completion.TrySetResult(Identity.GetAuthorizationClient(activity).GetAuthorizationResultFromIntent(data)); }
            catch (Java.Lang.Exception) { completion.TrySetException(new InvalidOperationException("Google recusou a autorização. Confira a configuração OAuth.")); }
        }
        return true;
    }
}

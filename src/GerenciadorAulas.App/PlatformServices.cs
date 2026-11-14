using System.Net;
using System.Net.Sockets;
using GerenciadorAulas.Core;
namespace GerenciadorAulas.App;
public sealed class PlatformTokenVault : ITokenVault
{
    public Task<string?> GetAsync(string key) => SecureStorage.Default.GetAsync(key);
    public Task SetAsync(string key, string value) => SecureStorage.Default.SetAsync(key, value);
    public Task RemoveAsync(string key) { SecureStorage.Default.Remove(key); return Task.CompletedTask; }
}
public sealed class SelectionContext
{
    public string? StudentId { get; set; }
    public string? YearId { get; set; }
    public string? RecoveryPassword { get; set; }
}
public static class DesktopGoogleLogin
{
    public static async Task ConnectAsync(GoogleAuthorization auth, string clientId, string? secret)
    {
#if WINDOWS
        if (string.IsNullOrWhiteSpace(clientId)) throw new ArgumentException("Configure o Client ID de aplicativo para computador no Google Cloud.");
        var socket = new TcpListener(IPAddress.Loopback,0); socket.Start(); var port = ((IPEndPoint)socket.LocalEndpoint).Port; socket.Stop();
        var redirect = $"http://127.0.0.1:{port}/";
        using var listener = new HttpListener(); listener.Prefixes.Add(redirect); listener.Start();
        var challenge = OAuthChallenge.Create();
        await Browser.Default.OpenAsync(GoogleAuthorization.AuthorizationUri(clientId,redirect,challenge),BrowserLaunchMode.External);
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(3));
        var context = await listener.GetContextAsync().WaitAsync(timeout.Token);
        var state=context.Request.QueryString["state"]??""; var code=context.Request.QueryString["code"];
        var response=System.Text.Encoding.UTF8.GetBytes("Autorização recebida. Volte ao GerenciadorAulas para verificar o resultado.");
        context.Response.ContentType="text/plain; charset=utf-8";await context.Response.OutputStream.WriteAsync(response);context.Response.Close();
        if (string.IsNullOrEmpty(code)) throw new InvalidOperationException("Autorização Google cancelada ou recusada.");
        await auth.ExchangeAsync(code,state,challenge,clientId,redirect,secret);
#else
        await Task.CompletedTask;
        throw new NotSupportedException("Login Android pendente da integração nativa Google Identity Services. O Google restringe callbacks de esquema personalizado; não usamos o callback de Windows no Android. Utilize backup local nesta versão.");
#endif
    }
}

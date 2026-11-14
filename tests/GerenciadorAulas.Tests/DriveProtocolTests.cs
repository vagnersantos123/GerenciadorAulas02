using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using GerenciadorAulas.Core;
using Xunit;
namespace GerenciadorAulas.Tests;
public sealed class DriveProtocolTests
{
    [Fact] public async Task UploadVerifiesBytesListsMetadataAndDownloadsIdenticalBackup()
    {
        var bytes=Encoding.UTF8.GetBytes("backup sintético de protocolo");
        var handler=new FakeGoogle(bytes);var vault=new Vault();
        await vault.SetAsync("google-tokens",JsonSerializer.Serialize(new OAuthTokens("synthetic-access-token",null,DateTimeOffset.UtcNow.AddHours(1))));
        using var http=new HttpClient(handler);var client=new GoogleDriveClient(http,new GoogleAuthorization(http,vault),vault);
        var package=new BackupPackage(bytes,new(1,DateTimeOffset.UtcNow,"Dispositivo fictício",42,[]));
        var uploaded=await client.UploadAsync(package);Assert.Equal(bytes.LongLength,uploaded.Size);
        var listed=await client.ListAsync();Assert.Single(listed);Assert.Equal(1,listed[0].Schema);Assert.Equal("Dispositivo fictício",listed[0].Device);
        Assert.Equal(bytes,await client.DownloadAsync(listed[0]));Assert.Equal("backups",await vault.GetAsync("drive-folder"));Assert.Equal(2,handler.CreatedFolders);
    }
    [Fact] public async Task IncorrectUploadChecksumIsRejected()
    {
        var bytes=new byte[]{1,2,3};var vault=new Vault();await vault.SetAsync("drive-folder","backups");await vault.SetAsync("google-tokens",JsonSerializer.Serialize(new OAuthTokens("synthetic-token",null,DateTimeOffset.UtcNow.AddHours(1))));
        using var http=new HttpClient(new FakeGoogle(bytes,true));var client=new GoogleDriveClient(http,new GoogleAuthorization(http,vault),vault);
        await Assert.ThrowsAsync<InvalidDataException>(()=>client.UploadAsync(new(bytes,new(1,DateTimeOffset.UtcNow,"Teste",1,[]))));
    }
    private sealed class FakeGoogle(byte[] expected,bool corrupt=false):HttpMessageHandler
    {
        public int CreatedFolders {get;private set;}
        private object Metadata()=>new {id="remote-backup",name="arquivo.gabackup",size=expected.LongLength.ToString(),createdTime=DateTimeOffset.UtcNow.ToString("O"),md5Checksum=corrupt?"wrong":Convert.ToHexString(MD5.HashData(expected)),appProperties=new {device="Dispositivo fictício",schema="1"}};
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken cancellationToken)
        {
            Assert.Equal("Bearer",request.Headers.Authorization?.Scheme);
            var uri=request.RequestUri!.ToString();
            if(uri.Contains("alt=media"))return new(HttpStatusCode.OK){Content=new ByteArrayContent(expected)};
            if(uri.Contains("uploadType=multipart"))
            {
                var multipart=Assert.IsAssignableFrom<MultipartContent>(request.Content);Assert.Equal(expected,await multipart.Last().ReadAsByteArrayAsync(cancellationToken));return Json(Metadata());
            }
            if(request.Method==HttpMethod.Post){CreatedFolders++;return Json(new {id=CreatedFolders==1?"app-folder":"backups"});}
            if(uri.Contains("fields=files(id)"))return Json(new {files=Array.Empty<object>()});
            return Json(new {files=new[]{Metadata()}});
        }
        private static HttpResponseMessage Json(object value)=>new(HttpStatusCode.OK){Content=new StringContent(JsonSerializer.Serialize(value),Encoding.UTF8,"application/json")};
    }
    private sealed class Vault:ITokenVault
    {
        private readonly Dictionary<string,string> data=[];
        public Task<string?> GetAsync(string key)=>Task.FromResult(data.GetValueOrDefault(key));
        public Task SetAsync(string key,string value){data[key]=value;return Task.CompletedTask;}
        public Task RemoveAsync(string key){data.Remove(key);return Task.CompletedTask;}
    }
}

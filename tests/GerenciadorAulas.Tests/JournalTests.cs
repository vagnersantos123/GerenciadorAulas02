using GerenciadorAulas.Core;
using Xunit;

namespace GerenciadorAulas.Tests;
public sealed class JournalTests : IAsyncLifetime
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "ga-test-" + Guid.NewGuid());
    private JournalStore store = null!;
    private EducationService service = null!;
    private Student student = null!;
    private SchoolYear year = null!;
    private Subject subject = null!;
    private List<Lesson> lessons = [];
    private const string Password = "Senha de teste 2026!";
    public async Task InitializeAsync()
    {
        store = new(root); service = new(store); await store.InitializeAsync();
        student = new() { Name = "Aluno fictício" }; await service.SaveAsync(student);
        year = new() { StudentId = student.Id, Name = "2026", Start = new(2026, 6, 29), End = new(2026, 8, 31), Breaks = [new(new(2026,7,1),new(2026,7,31),"Férias"),new(new(2026,8,3),new(2026,8,3),"Feriado")] }; await service.SaveAsync(year);
        subject = new() { YearId = year.Id, Name = "Matemática", LessonCount = 3 }; await service.SaveAsync(subject); lessons = await store.AllAsync<Lesson>();
    }
    public Task DisposeAsync() { if (Directory.Exists(root)) Directory.Delete(root,true); return Task.CompletedTask; }
    [Fact] public async Task OneLessonAcrossThreeDaysDoesNotCompleteAutomatically()
    {
        for (var i=0;i<3;i++) await service.SaveAsync(new StudySession { SubjectId=subject.Id,LessonIds=[lessons[0].Id],Date=new(2026,8,4+i),SessionFinished=true });
        Assert.Equal(3,(await store.AllAsync<StudySession>()).Count); Assert.False((await store.AllAsync<Lesson>())[0].Completed);
        lessons[0].Completed=true; await service.SaveAsync(lessons[0]); Assert.True((await store.AllAsync<Lesson>())[0].Completed);
    }
    [Fact] public async Task ThreeLessonsInOneSessionAndIdempotentSave()
    {
        var session=new StudySession {SubjectId=subject.Id,LessonIds=lessons.Select(l=>l.Id).ToArray(),Exercises=10,Correct=8};
        await service.SaveAsync(session); await service.SaveAsync(session);
        Assert.Single(await store.AllAsync<StudySession>()); Assert.Equal(3,(await store.AllAsync<StudySession>())[0].LessonIds.Length);
    }
    [Fact] public async Task PlanRespectsBreaksAndRemainsIndependentOfReality()
    {
        await service.GeneratePlanAsync(subject.Id); var plan=await store.AllAsync<PlannedLesson>();
        Assert.Equal(new DateOnly(2026,6,29),plan[0].OriginalDate); Assert.Equal(new DateOnly(2026,8,4),plan[2].OriginalDate);
        await service.SaveAsync(new StudySession {SubjectId=subject.Id,LessonIds=[lessons[0].Id],Date=new(2026,8,20)});
        Assert.Equal(plan[0].OriginalDate,(await store.AllAsync<PlannedLesson>())[0].OriginalDate);
        await Assert.ThrowsAsync<InvalidOperationException>(()=>service.GeneratePlanAsync(subject.Id));
        await Assert.ThrowsAsync<ArgumentException>(()=>service.SaveAsync(plan[0] with {OriginalDate=new(2026,8,1)}));
    }
    [Fact] public async Task CapacityFailureRollsBackWholePlan()
    {
        year.End=year.Start; await service.SaveAsync(year); await Assert.ThrowsAsync<ArgumentException>(()=>service.GeneratePlanAsync(subject.Id)); Assert.Empty(await store.AllAsync<PlannedLesson>());
    }
    [Fact] public async Task ReinforcementKeepsDifficultyAndMultipleLessons()
    {
        var d=new Difficulty {SubjectId=subject.Id,Theme="Frações",LessonIds=lessons.Select(l=>l.Id).ToArray()}; await service.SaveAsync(d);
        await service.SaveAsync(new StudySession {SubjectId=subject.Id,DifficultyId=d.Id,LessonIds=d.LessonIds,Minutes=20});
        d.Status=DifficultyStatus.Superada;d.FollowUps.Add(new(new(2026,8,10),"Revisão fictícia"));await service.SaveAsync(d);
        Assert.Single(await store.AllAsync<StudySession>()); Assert.Equal(DifficultyStatus.Superada,(await store.AllAsync<Difficulty>())[0].Status);
    }
    [Fact] public async Task ReopeningDatabasePreservesDataAndMigrationsAreRepeatable()
    {
        var reopened=new JournalStore(root);await reopened.InitializeAsync();await reopened.InitializeAsync(); Assert.Single(await reopened.AllAsync<Student>()); Assert.Equal(3,(await reopened.AllAsync<Lesson>()).Count);
    }
    [Fact] public async Task ForeignSubjectAndReferencedDeletionAreRejected()
    {
        var another=new Subject {YearId=year.Id,Name="História",LessonCount=1};await service.SaveAsync(another);
        await Assert.ThrowsAsync<ArgumentException>(()=>service.SaveAsync(new StudySession {SubjectId=another.Id,LessonIds=[lessons[0].Id]}));
        await Assert.ThrowsAsync<InvalidOperationException>(()=>store.DeleteAsync(student.Id));
        Assert.Empty(await store.AllAsync<StudySession>());
    }
    [Fact] public async Task BackupRoundTripIncludesDeduplicatedAttachmentsAndSafetyCopy()
    {
        var session=new StudySession {SubjectId=subject.Id,LessonIds=[lessons[0].Id]};await service.SaveAsync(session);
        var evidence=new EvidenceService(store);var content=new byte[]{1,2,3,4};
        await evidence.ImportAsync(session.Id,"atividade.pdf",new MemoryStream(content),false);await evidence.ImportAsync(session.Id,"outra.pdf",new MemoryStream(content),false);
        Assert.Single(Directory.GetFiles(store.AttachmentsPath));
        var backups=new BackupService(store);var package=await backups.CreateAsync(Password,"Dispositivo fictício");
        await service.SaveAsync(new Student {Name="Posterior"});
        await Assert.ThrowsAsync<InvalidOperationException>(()=>backups.RestoreAsync(package.Bytes,Password,true));
        var safety=await backups.RestoreAsync(package.Bytes,Password,true,true);
        try
        {
            Assert.True(File.Exists(safety));Assert.Single(await store.AllAsync<Student>());
            var restored=(await store.AllAsync<Evidence>())[0];Assert.Equal(content,await File.ReadAllBytesAsync(await evidence.VerifiedPathAsync(restored)));
        }
        finally { File.Delete(safety); }
    }
    [Fact] public async Task CorruptedBackupAndWrongPasswordNeverTouchLocalData()
    {
        var backup=new BackupService(store);var package=await backup.CreateAsync(Password,"Teste");var corrupt=package.Bytes.ToArray();corrupt[^1]^=1;
        await Assert.ThrowsAsync<InvalidDataException>(()=>backup.RestoreAsync(corrupt,Password,true,true));
        await Assert.ThrowsAsync<InvalidDataException>(()=>backup.InspectAsync(package.Bytes,"Senha errada qualquer"));Assert.Single(await store.AllAsync<Student>());
    }
    [Fact] public async Task RestoreRequiresConfirmation()
    {
        var backups=new BackupService(store);var package=await backups.CreateAsync(Password,"Teste");await Assert.ThrowsAsync<InvalidOperationException>(()=>backups.RestoreAsync(package.Bytes,Password,false));
    }
    [Fact] public async Task ConcurrentSameRecordSavesDoNotDuplicate()
    {
        var entry=new StudySession {SubjectId=subject.Id,LessonIds=[lessons[0].Id]};await Task.WhenAll(Enumerable.Range(0,8).Select(_=>service.SaveAsync(entry)));Assert.Single(await store.AllAsync<StudySession>());
    }
    [Fact] public async Task InvalidCountsRollback()
    {
        await Assert.ThrowsAsync<ArgumentException>(()=>service.SaveAsync(new StudySession {SubjectId=subject.Id,LessonIds=[lessons[0].Id],Exercises=2,Correct=3}));Assert.Empty(await store.AllAsync<StudySession>());
    }
    [Fact] public async Task DeletionAfterBackupAlsoRequiresNewerDataConfirmation()
    {
        var spare=new Student {Name="Excluir depois"};await service.SaveAsync(spare);var backups=new BackupService(store);var package=await backups.CreateAsync(Password,"Teste");await store.DeleteAsync(spare.Id);
        await Assert.ThrowsAsync<InvalidOperationException>(()=>backups.RestoreAsync(package.Bytes,Password,true));
    }
    [Fact] public async Task SubjectDeletionRemovesUnusedLessonsButProtectsHistoricalLessons()
    {
        var disposable=new Subject {YearId=year.Id,Name="Sem histórico",LessonCount=2};await service.SaveAsync(disposable);await store.DeleteAsync(disposable.Id);
        Assert.DoesNotContain(await store.AllAsync<Lesson>(),l=>l.SubjectId==disposable.Id);
        await service.SaveAsync(new StudySession {SubjectId=subject.Id,LessonIds=[lessons[0].Id]});await Assert.ThrowsAsync<InvalidOperationException>(()=>store.DeleteAsync(subject.Id));Assert.Equal(3,(await store.AllAsync<Lesson>()).Count);
    }
    [Fact] public async Task ReportEscapesUserTextAndOmitsSensitiveOccurrencesByDefault()
    {
        student.Name="<script>alert(1)</script>";await service.SaveAsync(student);await service.SaveAsync(new Occurrence {YearId=year.Id,Start=year.Start,End=year.Start,Title="Detalhe médico fictício",Sensitive=true});
        var reports=new ReportService(store);var html=await reports.HtmlAsync(year.Id,year.Start,year.End);Assert.DoesNotContain("<script>",html);Assert.Contains("&lt;script&gt;",html);Assert.DoesNotContain("Detalhe médico fictício",System.Net.WebUtility.HtmlDecode(html));Assert.Contains("Detalhe médico fictício",System.Net.WebUtility.HtmlDecode(await reports.HtmlAsync(year.Id,year.Start,year.End,true)));
    }
    [Fact] public async Task DriveFailureStaysPendingAndDoesNotMarkSuccess()
    {
        var vault=new MemoryVault();var coordinator=new BackupCoordinator(store,new BackupService(store),new FailingDrive(),vault);
        await Assert.ThrowsAsync<HttpRequestException>(()=>coordinator.RunAsync(Password,"Teste"));Assert.Contains("pendente",coordinator.Status);Assert.Null(await vault.GetAsync("drive-last-success"));
    }
    [Fact] public async Task OAuthRejectsMismatchedStateBeforeNetworkRequest()
    {
        var challenge=OAuthChallenge.Create();var auth=new GoogleAuthorization(new HttpClient(),new MemoryVault());
        await Assert.ThrowsAsync<InvalidOperationException>(()=>auth.ExchangeAsync("code","wrong",challenge,"configured-id","http://127.0.0.1:12345/"));
        Assert.Equal(43,challenge.Challenge.Length);Assert.Contains("code_challenge_method=S256",GoogleAuthorization.AuthorizationUri("id","redirect",challenge).ToString());
    }
    private sealed class FailingDrive:IBackupDrive
    {
        public Task<RemoteBackup> UploadAsync(BackupPackage package,CancellationToken ct=default)=>throw new HttpRequestException("Offline simulado");
        public Task<List<RemoteBackup>> ListAsync(CancellationToken ct=default)=>Task.FromResult(new List<RemoteBackup>());
        public Task<byte[]> DownloadAsync(RemoteBackup backup,CancellationToken ct=default)=>throw new HttpRequestException();
    }
    private sealed class MemoryVault:ITokenVault
    {
        private readonly Dictionary<string,string> values=[];
        public Task<string?> GetAsync(string key)=>Task.FromResult(values.GetValueOrDefault(key));
        public Task SetAsync(string key,string value){values[key]=value;return Task.CompletedTask;}
        public Task RemoveAsync(string key){values.Remove(key);return Task.CompletedTask;}
    }
}

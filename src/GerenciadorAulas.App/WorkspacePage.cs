using GerenciadorAulas.Core;
using CommunityToolkit.Mvvm.Input;
using DomainEntry=GerenciadorAulas.Core.Entry;
namespace GerenciadorAulas.App;
public sealed class WorkspacePage : ContentPage
{
    private readonly IServiceProvider services;
    private readonly string section;
    private readonly JournalStore store;
    private readonly SelectionContext context;
    private readonly WorkspaceViewModel vm=new();
    private readonly VerticalStackLayout body=new() {Spacing=12};
    private readonly VerticalStackLayout rows=new() {Spacing=10};
    private List<Student> students=[];
    private List<SchoolYear> years=[];
    private List<Subject> subjects=[];
    private List<Lesson> lessons=[];
    public WorkspacePage(IServiceProvider services,string section)
    {
        this.services=services;this.section=section;store=services.GetRequiredService<JournalStore>();context=services.GetRequiredService<SelectionContext>();Title=section;BindingContext=vm;
        var error=new Label {TextColor=Colors.DarkRed};error.SetBinding(Label.TextProperty,nameof(vm.Error));
        var busy=new ActivityIndicator {Color=Color.FromArgb("#14594E")};busy.SetBinding(ActivityIndicator.IsRunningProperty,nameof(vm.Busy));
        var stack=new VerticalStackLayout {Spacing=16,Padding=24,MaximumWidthRequest=1000,HorizontalOptions=LayoutOptions.Fill,Children={new Label {Text=section,FontSize=30,FontAttributes=FontAttributes.Bold,TextColor=Color.FromArgb("#14594E")},error,busy,body}};
        Content=new ScrollView {Content=stack};
    }
    protected override async void OnAppearing(){base.OnAppearing();await vm.Command(ReloadAsync).ExecuteAsync(null);}
    private Button Action(string text,Func<Task> action)=>new() {Text=text,Command=vm.Command(action)};
    private void Note(string text)=>body.Add(new Label {Text=text,TextColor=Color.FromArgb("#52645F"),FontSize=14});
    private string SubjectName(string id)=>subjects.Find(s=>s.Id==id)?.Name??"Matéria";
    private string LessonName(string id){var l=lessons.Find(l=>l.Id==id);return l is null?"Lição":$"{SubjectName(l.SubjectId)} · Lição {l.Number}";}
    private bool InYear(string subjectId)=>subjects.Any(s=>s.Id==subjectId&&s.YearId==context.YearId);
    private async Task ReloadAsync()
    {
        await store.InitializeAsync();students=await store.AllAsync<Student>();years=await store.AllAsync<SchoolYear>();subjects=await store.AllAsync<Subject>();lessons=await store.AllAsync<Lesson>();body.Clear();rows.Clear();
        if(!students.Any(s=>s.Id==context.StudentId))context.StudentId=students.FirstOrDefault()?.Id;
        var ownYears=years.Where(y=>y.StudentId==context.StudentId).ToList();if(!ownYears.Any(y=>y.Id==context.YearId))context.YearId=ownYears.LastOrDefault()?.Id;
        if(section!="Alunos")
        {
            var p=new Picker {Title="Aluno",ItemsSource=students.Select(s=>s.Name).ToList(),SelectedIndex=students.FindIndex(s=>s.Id==context.StudentId)};
            p.SelectedIndexChanged+=async(_,_)=>{if(p.SelectedIndex>=0&&context.StudentId!=students[p.SelectedIndex].Id){context.StudentId=students[p.SelectedIndex].Id;context.YearId=null;await vm.Command(ReloadAsync).ExecuteAsync(null);}};body.Add(p);
            var y=new Picker {Title="Ano letivo",ItemsSource=ownYears.Select(y=>y.Name).ToList(),SelectedIndex=ownYears.FindIndex(y=>y.Id==context.YearId)};
            y.SelectedIndexChanged+=async(_,_)=>{if(y.SelectedIndex>=0&&context.YearId!=ownYears[y.SelectedIndex].Id){context.YearId=ownYears[y.SelectedIndex].Id;await vm.Command(ReloadAsync).ExecuteAsync(null);}};body.Add(y);
        }
        switch(section)
        {
            case "Alunos":body.Add(Action("Adicionar aluno",()=>EditAsync(new Student())));foreach(var s in students)Row(s,s.Name,s.Notes);break;
            case "Anos letivos":
                Note("Cadastre o aluno primeiro. Configure os dias de estudo e todos os intervalos sem aula, incluindo férias de julho e feriados.");
                body.Add(Action("Adicionar ano letivo",()=>EditAsync(new SchoolYear {StudentId=context.StudentId??"",Name=DateTime.Today.Year.ToString()})));foreach(var y in ownYears)Row(y,y.Name,$"{y.Start:dd/MM/yyyy} a {y.End:dd/MM/yyyy} · {y.Breaks.Count} pausas");break;
            case "Matérias e lições":
                body.Add(Action("Adicionar matéria e gerar lições",()=>EditAsync(new Subject {YearId=context.YearId??""})));
                foreach(var s in subjects.Where(s=>s.YearId==context.YearId)){Row(s,s.Name,$"{lessons.Count(l=>l.SubjectId==s.Id&&l.Completed)}/{s.LessonCount} lições concluídas");body.Add(Action("Lições de "+s.Name,()=>ShowLessonsAsync(s)));}break;
            case "Planejamento":
                Note("A geração preserva datas originais. Ajustes de data só ocorrem após edição confirmada. Ocorrências não mudam o calendário automaticamente.");
                foreach(var s in subjects.Where(s=>s.YearId==context.YearId))body.Add(Action("Gerar planejamento · "+s.Name,async()=>{await services.GetRequiredService<EducationService>().GeneratePlanAsync(s.Id);await ReloadAsync();}));
                var plans=(await store.AllAsync<PlannedLesson>()).Where(p=>lessons.Any(l=>l.Id==p.LessonId&&InYear(l.SubjectId))).OrderBy(p=>p.OriginalDate).ToList();
                var plannedStudies=await store.AllAsync<StudySession>();
                Note("O andamento usa os estudos registrados. Adiantamento compara o primeiro estudo à data prevista; não indica conclusão automática.");
                Note($"{plans.Count} lições planejadas. Exibindo até 500 nesta versão.");foreach(var p in plans.Take(500)){var lesson=lessons.First(l=>l.Id==p.LessonId);Row(p,LessonName(p.LessonId),$"Original: {p.OriginalDate:dd/MM/yyyy} · Ajustada: {p.RevisedDate?.ToString("dd/MM/yyyy")??"—"} · {LessonPlanning.Describe(lesson,p,plannedStudies,DateOnly.FromDateTime(DateTime.Today))}\nTempo original: {p.EstimatedMinutes?.ToString()??"não informado"} min · ajustado: {p.RevisedEstimatedMinutes?.ToString()??"—"} min");}break;
            case "Registrar estudo":case "Histórico":
                body.Add(Action("Registrar sessão de estudo / reforço",()=>EditAsync(new StudySession {SubjectId=subjects.FirstOrDefault(s=>s.YearId==context.YearId)?.Id??""})));
                foreach(var s in (await store.AllAsync<StudySession>()).Where(s=>InYear(s.SubjectId)).OrderByDescending(s=>s.Date))Row(s,$"{s.Date:dd/MM/yyyy} · {SubjectName(s.SubjectId)}",$"Lições {string.Join(", ",lessons.Where(l=>s.LessonIds.Contains(l.Id)).Select(l=>l.Number))} · {s.Minutes} min · {s.Correct}/{s.Exercises} acertos{(s.DifficultyId is null?"":" · Reforço")}\n{SessionTiming.Describe(s)}\n{SessionTimeComparison.Describe(s)}\n{s.Notes}");break;
            case "Dificuldades e reforços":
                body.Add(Action("Registrar dificuldade",()=>EditAsync(new Difficulty {SubjectId=subjects.FirstOrDefault(s=>s.YearId==context.YearId)?.Id??""})));
                foreach(var d in (await store.AllAsync<Difficulty>()).Where(d=>InYear(d.SubjectId))) {Row(d,d.Theme,$"{SubjectName(d.SubjectId)} · {d.Status}\n{d.Description}");body.Add(Action("Registrar reforço · "+d.Theme,()=>EditAsync(new StudySession {SubjectId=d.SubjectId,DifficultyId=d.Id,LessonIds=d.LessonIds})));}break;
            case "Atividades e avaliações":
                body.Add(Action("Adicionar atividade ou avaliação",()=>EditAsync(new Activity {SubjectId=subjects.FirstOrDefault(s=>s.YearId==context.YearId)?.Id??""})));
                foreach(var a in (await store.AllAsync<Activity>()).Where(a=>InYear(a.SubjectId)).OrderByDescending(a=>a.Date))Row(a,a.Title,$"{a.Date:dd/MM/yyyy} · {SubjectName(a.SubjectId)} · {a.Result}\n{a.Notes}");break;
            case "Ocorrências":
                body.Add(Action("Registrar ocorrência",()=>EditAsync(new Occurrence {YearId=context.YearId??""})));
                foreach(var o in (await store.AllAsync<Occurrence>()).Where(o=>o.YearId==context.YearId))Row(o,o.Title,$"{o.Start:dd/MM/yyyy} a {o.End:dd/MM/yyyy}{(o.Sensitive?" · Sensível":"")}\n{o.Notes}");break;
            case "Evidências":
                Note("Para anexar, abra o menu de um estudo, dificuldade, atividade ou ocorrência. Os arquivos ficam no armazenamento privado do aplicativo (até 50 MB por arquivo).");
                var scoped=(await store.AllAsync<StudySession>()).Where(s=>InYear(s.SubjectId)).Select(s=>s.Id).Concat((await store.AllAsync<Activity>()).Where(a=>InYear(a.SubjectId)).Select(a=>a.Id)).Concat((await store.AllAsync<Difficulty>()).Where(d=>InYear(d.SubjectId)).Select(d=>d.Id)).Concat((await store.AllAsync<Occurrence>()).Where(o=>o.YearId==context.YearId).Select(o=>o.Id)).ToHashSet();
                foreach(var e in (await store.AllAsync<Evidence>()).Where(e=>scoped.Contains(e.RecordId)))Row(e,e.FileName,$"{e.Size/1024d:F1} KB{(e.Sensitive?" · Sensível":"")}");break;
            case "Relatórios":BuildReports();break;
            case "Configurações":
                Note("Armazenamento local privado: SQLite e arquivos separados. A desinstalação pode apagar os dados. Exporte backups regularmente. O banco antigo não é migrado automaticamente; mantenha uma cópia dele.");
                Note("Versão 1.0 · esquema "+JournalStore.SchemaVersion+" · Interface em português brasileiro. Use o menu para cadastrar aluno → ano letivo → matéria → estudos.");body.Add(Action("Atualizar tela",ReloadAsync));break;
            case "Backup Google Drive":await BuildBackupAsync();break;
            default:
                var sessions=(await store.AllAsync<StudySession>()).Where(s=>InYear(s.SubjectId)).ToArray();var ownLessons=lessons.Where(l=>InYear(l.SubjectId)).ToArray();
                Note("Organize o que foi planejado e registre o que realmente aconteceu. Os dados continuam disponíveis sem internet.");
                Note($"{students.Count} alunos · {subjects.Count(s=>s.YearId==context.YearId)} matérias neste ano\n{ownLessons.Count(l=>l.Completed)} de {ownLessons.Length} lições concluídas · {sessions.Length} sessões · {sessions.Sum(s=>(long)s.Minutes)} minutos");
                body.Add(Action("Novo estudo",()=>EditAsync(new StudySession {SubjectId=subjects.FirstOrDefault(s=>s.YearId==context.YearId)?.Id??""})));Note("Comece pelo menu Alunos. Depois, configure Anos letivos e Matérias e lições.");break;
        }
        body.Add(rows);
    }
    private void Row(DomainEntry entry,string title,string subtitle)
    {
        var card=new VerticalStackLayout {Spacing=6,Children={new Label {Text=title,FontSize=18,FontAttributes=FontAttributes.Bold},new Label {Text=subtitle,TextColor=Color.FromArgb("#52645F")},Action("Opções",()=>OptionsAsync(entry))}};
        rows.Add(new Border {Stroke=Color.FromArgb("#DCE5DF"),StrokeShape=new Microsoft.Maui.Controls.Shapes.RoundRectangle {CornerRadius=14},BackgroundColor=Colors.White,Padding=16,Content=card});
    }
    private async Task EditAsync(DomainEntry entry)
    {
        var page=new EntryForm(services,entry);await page.PrepareAsync(ReloadAsync);await Navigation.PushModalAsync(new NavigationPage(page));
    }
    private async Task ShowLessonsAsync(Subject subject)
    {
        var stack=new VerticalStackLayout {Padding=24,Spacing=10};var page=new ContentPage {Title=subject.Name,Content=new ScrollView {Content=stack}};
        var search=new SearchBar {Placeholder="Filtrar por número ou título"};stack.Add(search);var list=new VerticalStackLayout {Spacing=8};stack.Add(list);
        void Populate(){list.Clear();foreach(var lesson in lessons.Where(l=>l.SubjectId==subject.Id&&(string.IsNullOrWhiteSpace(search.Text)||l.Number.ToString()==search.Text||l.Title.Contains(search.Text,StringComparison.CurrentCultureIgnoreCase))).OrderBy(l=>l.Number).Take(200))list.Add(Action($"{lesson.Number} · {lesson.Title} · {(lesson.Completed?"Concluída":"Pendente")}",()=>EditAsync(lesson)));}
        search.TextChanged+=(_,_)=>Populate();Populate();stack.Add(new Label {Text="Até 200 resultados. Use o filtro para localizar qualquer lição. A conclusão é editada explicitamente."});await Navigation.PushAsync(page);
    }
    private async Task OptionsAsync(DomainEntry entry)
    {
        var options=entry is Evidence?new[]{"Abrir arquivo","Excluir"}:entry is StudySession or Difficulty or Activity or Occurrence?new[]{"Editar","Anexar arquivo","Excluir"}:new[]{"Editar","Excluir"};
        var chosen=await DisplayActionSheetAsync("Registro","Cancelar",null,options);
        if(chosen=="Editar"){await EditAsync(entry);return;}
        if(chosen=="Excluir")
        {
            if(await DisplayAlertAsync("Excluir registro?","Registros com dependentes serão preservados. A exclusão não pode ser desfeita sem restaurar um backup.","Excluir","Cancelar")){await store.DeleteAsync(entry.Id);await ReloadAsync();}return;
        }
        if(chosen=="Anexar arquivo")
        {
            var file=await FilePicker.Default.PickAsync(new PickOptions {PickerTitle="Selecionar evidência"});if(file is null)return;
            var sensitive=await DisplayAlertAsync("Documento sensível?","Contém dados médicos ou outros detalhes privados?","Sim","Não");
            await using var stream=await file.OpenReadAsync();await services.GetRequiredService<EvidenceService>().ImportAsync(entry.Id,file.FileName,stream,sensitive||entry is Occurrence {Sensitive:true});await ReloadAsync();
        }
        if(chosen=="Abrir arquivo"&&entry is Evidence e)
        {
            if(e.Sensitive&&!await DisplayAlertAsync("Documento sensível","Abrir este documento em outro aplicativo pode expor seu conteúdo.","Abrir","Cancelar"))return;
            var path=await services.GetRequiredService<EvidenceService>().VerifiedPathAsync(e);var temp=Path.Combine(FileSystem.CacheDirectory,Guid.NewGuid().ToString("N"));Directory.CreateDirectory(temp);var target=Path.Combine(temp,Path.GetFileName(e.FileName));File.Copy(path,target);await Launcher.Default.OpenAsync(new OpenFileRequest {File=new ReadOnlyFile(target)});
        }
    }
    private void BuildReports()
    {
        Note("Relatório por aluno e ano selecionados. Exportação HTML imprimível; no Windows, abra no navegador e use Imprimir → Salvar como PDF. Anexos não são incorporados.");
        var year=years.Find(y=>y.Id==context.YearId);var start=new DatePicker {Date=year?.Start.ToDateTime(TimeOnly.MinValue)??DateTime.Today};var end=new DatePicker {Date=year?.End.ToDateTime(TimeOnly.MinValue)??DateTime.Today};body.Add(new Label {Text="Início"});body.Add(start);body.Add(new Label {Text="Término"});body.Add(end);var sensitive=new Switch();body.Add(new Label {Text="Incluir detalhes sensíveis de ocorrências"});body.Add(sensitive);
        body.Add(Action("Exportar relatório",async()=>{if(context.YearId is null)throw new ArgumentException("Selecione um ano letivo.");var html=await services.GetRequiredService<ReportService>().HtmlAsync(context.YearId,DateOnly.FromDateTime(start.Date??DateTime.Today),DateOnly.FromDateTime(end.Date??DateTime.Today),sensitive.IsToggled);var path=Path.Combine(FileSystem.CacheDirectory,$"relatorio-{DateTime.Now:yyyyMMdd-HHmmss}.html");await File.WriteAllTextAsync(path,html);await Share.Default.RequestAsync(new ShareFileRequest("Relatório educacional",new ShareFile(path)));}));
    }
    private async Task BuildBackupAsync()
    {
        Note("Backup completo criptografado. Use uma senha de recuperação com 12 ou mais caracteres. Ela é mantida somente nesta sessão; sem ela, não há recuperação dos arquivos. Limite desta versão: 256 MB por backup.");
        var password=new Microsoft.Maui.Controls.Entry {Placeholder="Senha de recuperação",IsPassword=true,Text=context.RecoveryPassword};body.Add(password);password.TextChanged+=(_,_)=>context.RecoveryPassword=password.Text;
        var backup=services.GetRequiredService<BackupService>();var coordinator=services.GetRequiredService<BackupCoordinator>();var drive=services.GetRequiredService<GoogleDriveClient>();var vault=services.GetRequiredService<ITokenVault>();
        Note(coordinator.Status);var last=await vault.GetAsync("drive-last-success");if(last is not null)Note("Último envio verificado (UTC): "+last);
        body.Add(Action("Exportar backup local",async()=>{var package=await backup.CreateAsync(password.Text??"",DeviceInfo.Name);var path=Path.Combine(FileSystem.CacheDirectory,$"GerenciadorAulas-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}.gabackup");await File.WriteAllBytesAsync(path,package.Bytes);await Share.Default.RequestAsync(new ShareFileRequest("Salvar backup",new ShareFile(path)));}));
        body.Add(Action("Restaurar arquivo local",async()=>{var file=await FilePicker.Default.PickAsync(new PickOptions {PickerTitle="Selecionar arquivo .gabackup"});if(file is null)return;await using var stream=await file.OpenReadAsync();var bytes=await ReadBackupAsync(stream);await RestoreAsync(bytes,password.Text??"");}));
        body.Add(Action("Exportar cópia de segurança anterior",async()=>
        {
            var copies=Directory.GetFiles(Path.GetDirectoryName(store.DirectoryPath)!,"seguranca-*.gabackup").OrderDescending().ToArray();
            if(copies.Length==0){await DisplayAlertAsync("Cópias de segurança","Nenhuma restauração criou cópia de segurança neste dispositivo.","OK");return;}
            var names=copies.Select(Path.GetFileName).ToArray();var chosen=await DisplayActionSheetAsync("Escolher cópia criptografada","Cancelar",null,names!);var index=Array.IndexOf(names,chosen);
            if(index>=0)await Share.Default.RequestAsync(new ShareFileRequest("Salvar cópia de segurança",new ShareFile(copies[index])));
        }));
        Note("Google Drive: a configuração OAuth e o primeiro login dependem do proprietário. Login Windows usa navegador + PKCE + callback loopback. Login Android nativo está pendente nesta versão; backup local funciona nas duas plataformas.");
        var id=new Microsoft.Maui.Controls.Entry {Placeholder="Client ID OAuth para computador",Text=Preferences.Default.Get("google-client-id","")};var secret=new Microsoft.Maui.Controls.Entry {Placeholder="Client secret do cliente desktop (se exigido pelo Google)",IsPassword=true};body.Add(id);body.Add(secret);
        body.Add(Action("Conectar conta Google (Windows)",async()=>{await DesktopGoogleLogin.ConnectAsync(services.GetRequiredService<GoogleAuthorization>(),id.Text??"",secret.Text);Preferences.Default.Set("google-client-id",id.Text??"");await DisplayAlertAsync("Conta conectada","A permissão drive.file está pronta para uso. Faça um backup para validar o envio real.","OK");}));
        body.Add(Action("Desconectar conta Google",async()=>{coordinator.Disable();await services.GetRequiredService<GoogleAuthorization>().DisconnectAsync();await ReloadAsync();}));
        body.Add(Action("Fazer backup agora no Drive",async()=>{await coordinator.RunAsync(password.Text??"",DeviceInfo.Name);await ReloadAsync();}));
        var auto=new Switch {IsToggled=coordinator.Enabled};body.Add(new Label {Text="Backup automático enquanto o aplicativo está aberto (após 30 s sem alterações)"});body.Add(auto);auto.Toggled+=(_,e)=>{if(e.Value)coordinator.Enable();else coordinator.Disable();};
        body.Add(Action("Listar backups do Google Drive",async()=>
        {
            var list=await drive.ListAsync();if(list.Count==0){await DisplayAlertAsync("Backups","Nenhum backup acessível para este aplicativo. Confira também a permissão da pasta no outro dispositivo.","OK");return;}
            var labels=list.Select((b,i)=>$"{i+1}. {b.CreatedUtc.ToLocalTime():g} · {b.Device} · {b.Size/1024d/1024:F1} MB · esquema {b.Schema}").ToArray();
            var selected=await DisplayActionSheetAsync("Restaurar backup","Cancelar",null,labels);var index=Array.IndexOf(labels,selected);if(index>=0)await RestoreAsync(await drive.DownloadAsync(list[index]),password.Text??"");
        }));
    }
    private static async Task<byte[]> ReadBackupAsync(Stream input)
    {
        using var output=new MemoryStream();var buffer=new byte[81920];int n;while((n=await input.ReadAsync(buffer))>0){if(output.Length+n>260L*1024*1024)throw new InvalidDataException("Backup excede o limite.");await output.WriteAsync(buffer.AsMemory(0,n));}return output.ToArray();
    }
    private async Task RestoreAsync(byte[] bytes,string password)
    {
        var backups=services.GetRequiredService<BackupService>();var manifest=await backups.InspectAsync(bytes,password);
        if(!await DisplayAlertAsync("Substituir todos os registros locais?",$"Backup de {manifest.CreatedUtc.ToLocalTime():g}, dispositivo {manifest.Device}. Uma cópia de segurança criptografada será criada antes da substituição. Não há mesclagem.","Substituir","Cancelar"))return;
        string safety;
        try{safety=await backups.RestoreAsync(bytes,password,true);}
        catch(InvalidOperationException ex) when(ex.Message.Contains("posteriores"))
        {
            if(!await DisplayAlertAsync("Há dados locais mais novos","Você perderá essas alterações na base ativa. A cópia de segurança preservará o estado atual. Restaurar mesmo assim?","Restaurar mesmo assim","Cancelar"))return;
            safety=await backups.RestoreAsync(bytes,password,true,true);
        }
        await services.GetRequiredService<ITokenVault>().RemoveAsync("drive-last-revision");
        context.StudentId=null;context.YearId=null;await ReloadAsync();await DisplayAlertAsync("Restauração concluída","Banco e anexos restaurados. Cópia de segurança local: "+Path.GetFileName(safety),"OK");
    }
}

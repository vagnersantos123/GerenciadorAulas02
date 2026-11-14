using GerenciadorAulas.Core;
using Entry=GerenciadorAulas.Core.Entry;
namespace GerenciadorAulas.App;
public sealed class EntryForm : ContentPage
{
    private readonly VerticalStackLayout fields = new() { Spacing=12,Padding=24,MaximumWidthRequest=720,HorizontalOptions=LayoutOptions.Fill };
    private readonly List<Action> apply=[];
    private readonly WorkspaceViewModel vm=new();
    private readonly Entry record;
    private readonly IServiceProvider services;
    public EntryForm(IServiceProvider services,Entry record) { this.services=services;this.record=record;Title="Editar registro";BindingContext=vm;Content=new ScrollView {Content=fields}; }
    private void Caption(string text)=>fields.Add(new Label {Text=text,FontAttributes=FontAttributes.Bold,TextColor=Color.FromArgb("#14594E")});
    private void Text(string label,string value,Action<string> set,bool multiline=false)
    {
        Caption(label);
        if(multiline){var c=new Editor {Text=value,AutoSize=EditorAutoSizeOption.TextChanges,MinimumHeightRequest=90};fields.Add(c);apply.Add(()=>set(c.Text??""));}
        else{var c=new Microsoft.Maui.Controls.Entry {Text=value};fields.Add(c);apply.Add(()=>set(c.Text??""));}
    }
    private void Number(string label,int value,Action<int> set)=>Text(label,value.ToString(),v=>{if(!int.TryParse(v,out var n))throw new ArgumentException(label+": informe um número inteiro.");set(n);});
    private void Date(string label,DateOnly value,Action<DateOnly> set){Caption(label);var c=new DatePicker {Date=value.ToDateTime(TimeOnly.MinValue)};fields.Add(c);apply.Add(()=>set(DateOnly.FromDateTime(c.Date??DateTime.Today)));}
    private void Toggle(string label,bool value,Action<bool> set){Caption(label);var c=new Switch {IsToggled=value};fields.Add(c);apply.Add(()=>set(c.IsToggled));}
    private Picker Pick<T>(string label,List<T> choices,T? selected,Func<T,string> name,Action<T> set) where T:class
    {
        Caption(label);var p=new Picker {Title=label};foreach(var c in choices)p.Items.Add(name(c));p.SelectedIndex=selected is null ? -1 : choices.IndexOf(selected);fields.Add(p);
        apply.Add(()=>{if(p.SelectedIndex<0)throw new ArgumentException("Selecione: "+label);set(choices[p.SelectedIndex]);});return p;
    }
    private void Days(DayOfWeek[] days,Action<DayOfWeek[]> set)=>Text("Dias da semana (0 domingo, 1 segunda … 6 sábado)",string.Join(",",days.Select(d=>(int)d)),v=>set(v.Split(',',StringSplitOptions.TrimEntries|StringSplitOptions.RemoveEmptyEntries).Select(x=>int.TryParse(x,out var n)&&n is >=0 and <=6?(DayOfWeek)n:throw new ArgumentException("Dia inválido.")).Distinct().ToArray()));
    public async Task PrepareAsync(Func<Task> saved)
    {
        var store=services.GetRequiredService<JournalStore>();var context=services.GetRequiredService<SelectionContext>();
        var students=await store.AllAsync<Student>();var years=await store.AllAsync<SchoolYear>();var subjects=await store.AllAsync<Subject>();var lessons=await store.AllAsync<Lesson>();
        var currentYear=context.YearId;var available=subjects.Where(s=>s.YearId==currentYear).ToList();
        void SubjectChoice(string id,Action<string> set)=>Pick("Matéria",available,available.Find(s=>s.Id==id),s=>s.Name,s=>set(s.Id));
        void LessonNumbers(string subjectId,string[] ids,Func<string> getSubject,Action<string[]> set)=>Text("Lições (números separados por vírgula)",string.Join(",",lessons.Where(l=>ids.Contains(l.Id)).Select(l=>l.Number)),v=>
        {
            var numbers=v.Split(',',StringSplitOptions.TrimEntries|StringSplitOptions.RemoveEmptyEntries).Select(x=>int.TryParse(x,out var n)?n:throw new ArgumentException("Use números de lições separados por vírgula.")).ToArray();
            if(numbers.Distinct().Count()!=numbers.Length)throw new ArgumentException("Lições repetidas.");
            set(numbers.Select(n=>lessons.SingleOrDefault(l=>l.SubjectId==getSubject()&&l.Number==n)?.Id??throw new ArgumentException($"Lição {n} inexistente na matéria selecionada.")).ToArray());
        });
        switch(record)
        {
            case Student s:Text("Nome",s.Name,v=>s.Name=v);Text("Observações",s.Notes,v=>s.Notes=v,true);break;
            case SchoolYear y:
                Pick("Aluno",students,students.Find(s=>s.Id==y.StudentId),s=>s.Name,s=>y.StudentId=s.Id);Text("Nome do ano letivo",y.Name,v=>y.Name=v);Date("Início",y.Start,v=>y.Start=v);Date("Término",y.End,v=>y.End=v);Days(y.StudyDays,v=>y.StudyDays=v);
                Text("Férias, feriados e pausas — uma linha por intervalo: AAAA-MM-DD | AAAA-MM-DD | motivo",string.Join("\n",y.Breaks.Select(b=>$"{b.Start:yyyy-MM-dd} | {b.End:yyyy-MM-dd} | {b.Reason}")),v=>y.Breaks=v.Split('\n',StringSplitOptions.RemoveEmptyEntries).Select(line=>{var parts=line.Split('|',StringSplitOptions.TrimEntries);if(parts.Length!=3||!DateOnly.TryParseExact(parts[0],"yyyy-MM-dd",out var start)||!DateOnly.TryParseExact(parts[1],"yyyy-MM-dd",out var end))throw new ArgumentException("Intervalo de pausa inválido.");return new DateRange(start,end,parts[2]);}).ToList(),true);break;
            case Subject s:
                Pick("Ano letivo",years.Where(y=>y.StudentId==context.StudentId).ToList(),years.Find(y=>y.Id==s.YearId),y=>y.Name,y=>s.YearId=y.Id);Text("Matéria",s.Name,v=>s.Name=v);Number("Quantidade de lições",s.LessonCount,v=>s.LessonCount=v);Number("Lições por dia de estudo",s.LessonsPerDay,v=>s.LessonsPerDay=v);Days(s.StudyDays,v=>s.StudyDays=v);break;
            case Lesson l:Caption("Lição "+l.Number);Text("Título ou tema",l.Title,v=>l.Title=v);Text("Observações",l.Notes,v=>l.Notes=v,true);Toggle("Lição concluída (decisão explícita)",l.Completed,v=>l.Completed=v);break;
            case PlannedLesson p:
                Caption($"Data original preservada: {p.OriginalDate:dd/MM/yyyy}");Date("Nova data confirmada",p.RevisedDate??p.OriginalDate,v=>p.RevisedDate=v);break;
            case StudySession s:
                SubjectChoice(s.SubjectId,v=>s.SubjectId=v);Date("Data real",s.Date,v=>s.Date=v);LessonNumbers(s.SubjectId,s.LessonIds,()=>s.SubjectId,v=>s.LessonIds=v);
                Text("Horário inicial (HH:mm, opcional)",s.StartTime?.ToString("HH:mm")??"",v=>s.StartTime=string.IsNullOrWhiteSpace(v)?null:TimeOnly.TryParseExact(v,"HH:mm",out var t)?t:throw new ArgumentException("Horário inválido."));
                Number("Duração em minutos",s.Minutes,v=>s.Minutes=v);Number("Exercícios",s.Exercises,v=>s.Exercises=v);Number("Acertos",s.Correct,v=>s.Correct=v);
                var difficulties=await store.AllAsync<Difficulty>();var choices=difficulties.Where(d=>available.Any(a=>a.Id==d.SubjectId)).ToList();
                Caption("Reforço de dificuldade (opcional)");var dPicker=new Picker {Title="Sem reforço"};dPicker.Items.Add("Sem reforço");foreach(var d in choices)dPicker.Items.Add(d.Theme);dPicker.SelectedIndex=choices.FindIndex(d=>d.Id==s.DifficultyId)+1;fields.Add(dPicker);apply.Add(()=>s.DifficultyId=dPicker.SelectedIndex<=0?null:choices[dPicker.SelectedIndex-1].Id);
                Toggle("Sessão encerrada (não conclui as lições)",s.SessionFinished,v=>s.SessionFinished=v);Text("Observações",s.Notes,v=>s.Notes=v,true);break;
            case Difficulty d:
                SubjectChoice(d.SubjectId,v=>d.SubjectId=v);Date("Identificação",d.Date,v=>d.Date=v);Text("Tema",d.Theme,v=>d.Theme=v);LessonNumbers(d.SubjectId,d.LessonIds,()=>d.SubjectId,v=>d.LessonIds=v);Text("Dificuldade",d.Description,v=>d.Description=v,true);
                Pick("Situação",Enum.GetValues<DifficultyStatus>().Select(s=>new StatusChoice(s)).ToList(),new StatusChoice(d.Status),s=>s.Value.ToString(),s=>d.Status=s.Value);
                Caption("Histórico de observações");foreach(var f in d.FollowUps)fields.Add(new Label {Text=$"{f.Date:dd/MM/yyyy} · {f.Notes}"});var prior=d.FollowUps.ToList();Text("Nova observação de acompanhamento (opcional)","",v=>{d.FollowUps=prior.ToList();if(!string.IsNullOrWhiteSpace(v))d.FollowUps.Add(new(DateOnly.FromDateTime(DateTime.Today),v));},true);break;
            case Activity a:SubjectChoice(a.SubjectId,v=>a.SubjectId=v);Date("Data",a.Date,v=>a.Date=v);Text("Título e tipo de atividade",a.Title,v=>a.Title=v);Text("Resultado",a.Result,v=>a.Result=v);Text("Observações",a.Notes,v=>a.Notes=v,true);break;
            case Occurrence o:
                o.YearId=context.YearId??o.YearId;Text("Ocorrência",o.Title,v=>o.Title=v);Date("Início",o.Start,v=>o.Start=v);Date("Fim",o.End,v=>o.End=v);Toggle("Informação sensível (ocultar em relatórios)",o.Sensitive,v=>o.Sensitive=v);Text("Observações",o.Notes,v=>o.Notes=v,true);
                var selected=new List<(Subject,CheckBox)>();Caption("Matérias afetadas (nenhuma = todo o ano letivo)");foreach(var s in available){var check=new CheckBox {IsChecked=o.SubjectIds.Contains(s.Id)};fields.Add(new HorizontalStackLayout {Children={check,new Label {Text=s.Name,VerticalOptions=LayoutOptions.Center}}});selected.Add((s,check));}apply.Add(()=>o.SubjectIds=selected.Where(x=>x.Item2.IsChecked).Select(x=>x.Item1.Id).ToArray());break;
        }
        var error=new Label {TextColor=Colors.DarkRed};error.SetBinding(Label.TextProperty,nameof(vm.Error));fields.Add(error);
        var save=new Button {Text="Salvar registro",Command=vm.Command(async()=>{foreach(var action in apply)action();await services.GetRequiredService<EducationService>().SaveAsync(record);await saved();await Navigation.PopModalAsync();})};fields.Add(save);
        fields.Add(new Button {Text="Cancelar",Command=vm.Command(async()=>await Navigation.PopModalAsync())});
    }
    private sealed record StatusChoice(DifficultyStatus Value);
}

using System.Net;
using System.Text;
namespace GerenciadorAulas.Core;
public sealed class ReportService(JournalStore store)
{
    public async Task<string> HtmlAsync(string yearId, DateOnly start, DateOnly end, bool includeSensitive = false)
    {
        if (end < start) throw new ArgumentException("Intervalo inválido.");
        var year = (await store.AllAsync<SchoolYear>()).Single(y => y.Id == yearId);
        var student = (await store.AllAsync<Student>()).Single(s => s.Id == year.StudentId);
        var subjects = (await store.AllAsync<Subject>()).Where(s => s.YearId == yearId).ToDictionary(s => s.Id);
        var lessons = (await store.AllAsync<Lesson>()).Where(l => subjects.ContainsKey(l.SubjectId)).ToDictionary(l => l.Id);
        var sessions = (await store.AllAsync<StudySession>()).Where(s => subjects.ContainsKey(s.SubjectId) && s.Date >= start && s.Date <= end).OrderBy(s => s.Date).ToArray();
        var plans = (await store.AllAsync<PlannedLesson>()).Where(p => lessons.ContainsKey(p.LessonId) && p.OriginalDate >= start && p.OriginalDate <= end).ToArray();
        static string H(object? value) => WebUtility.HtmlEncode(value?.ToString() ?? "");
        var html = new StringBuilder("<!doctype html><html lang='pt-BR'><meta charset='utf-8'><title>Relatório educacional</title><style>body{font:16px system-ui;max-width:1000px;margin:40px auto;color:#163b35}table{border-collapse:collapse;width:100%}td,th{padding:10px;text-align:left;border-bottom:1px solid #ccc}h2{margin-top:32px}@media print{body{margin:1cm}tr{break-inside:avoid}}</style><h1>GerenciadorAulas · Relatório educacional</h1>");
        html.Append($"<p>{H(student.Name)} · {H(year.Name)} · {start:dd/MM/yyyy} a {end:dd/MM/yyyy}</p><p>{sessions.Length} sessões · {sessions.Sum(s => (long)s.Minutes)} minutos · {sessions.Sum(s => s.Exercises)} exercícios · {sessions.Sum(s => s.Correct)} acertos</p>");
        html.Append("<h2>Matérias e planejamento</h2><table><tr><th>Matéria</th><th>Planejadas no intervalo</th><th>Concluídas atualmente / total</th></tr>");
        foreach (var subject in subjects.Values) html.Append($"<tr><td>{H(subject.Name)}</td><td>{plans.Count(p => lessons[p.LessonId].SubjectId == subject.Id)}</td><td>{lessons.Values.Count(l => l.SubjectId == subject.Id && l.Completed)} / {subject.LessonCount}</td></tr>");
        html.Append("</table><p>A conclusão representa a situação atual, não uma conclusão presumida na data planejada.</p><h2>Estudos realizados</h2>");
        html.Append("<p>A comparação usa o tempo total de cada sessão, sem distribuição presumida por lição. Diferenças de tempo não avaliam desempenho.</p>");
        foreach (var s in sessions) html.Append($"<p><b>{s.Date:dd/MM/yyyy} · {H(subjects[s.SubjectId].Name)}</b> · Lições {H(string.Join(", ",s.LessonIds.Select(id => lessons[id].Number)))} · {s.Minutes} min · {s.Correct}/{s.Exercises} acertos{(s.DifficultyId is null ? "" : " · Reforço")}<br>{H(SessionTiming.Describe(s))}<br>{H(SessionTimeComparison.Describe(s))}<br>{H(s.Notes)}</p>");
        html.Append("<h2>Previsto e realizado</h2>");
        foreach (var p in plans) { var l = lessons[p.LessonId]; html.Append($"<p>{H(subjects[l.SubjectId].Name)} · Lição {l.Number} · Original {p.OriginalDate:dd/MM/yyyy} · Ajustada {H(p.RevisedDate?.ToString("dd/MM/yyyy"))} · Sessões: {H(string.Join(", ",sessions.Where(s => s.LessonIds.Contains(l.Id)).Select(s => s.Date.ToString("dd/MM/yyyy"))))}</p>"); }
        html.Append("<h2>Avaliações e atividades</h2>");
        foreach (var a in (await store.AllAsync<Activity>()).Where(a => subjects.ContainsKey(a.SubjectId) && a.Date >= start && a.Date <= end)) html.Append($"<p>{a.Date:dd/MM/yyyy} · {H(a.Title)} · {H(a.Result)} · {H(a.Notes)}</p>");
        html.Append("<h2>Dificuldades e acompanhamento</h2>");
        foreach (var d in (await store.AllAsync<Difficulty>()).Where(d => subjects.ContainsKey(d.SubjectId) && d.Date <= end)) html.Append($"<p>{H(d.Theme)} · {H(d.Status)} · {H(d.Description)}<br>{H(string.Join("; ",d.FollowUps.Where(f => f.Date >= start && f.Date <= end).Select(f => $"{f.Date:dd/MM/yyyy}: {f.Notes}")))}</p>");
        html.Append("<h2>Ocorrências</h2>");
        foreach (var o in (await store.AllAsync<Occurrence>()).Where(o => o.YearId == yearId && o.End >= start && o.Start <= end && (includeSensitive || !o.Sensitive))) html.Append($"<p>{o.Start:dd/MM/yyyy} a {o.End:dd/MM/yyyy} · {H(o.Title)} · {H(o.Notes)}</p>");
        html.Append("<p>Documentos anexos não são incorporados ao relatório. Dados sensíveis ficam ocultos por padrão. Registros familiares sem certificação de autenticidade ou declaração automática de validade jurídica.</p></html>");
        return html.ToString();
    }
}

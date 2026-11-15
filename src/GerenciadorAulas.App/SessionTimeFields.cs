using GerenciadorAulas.Core;

namespace GerenciadorAulas.App;

public sealed class SessionTimeFields : ContentView
{
    private readonly StudySession original;
    private readonly bool isNew;
    private readonly SessionTiming timing;
    private readonly DatePicker startDate = DateControl();
    private readonly TimePicker startTime = new() { Format = "HH:mm", AutomationId = "SessionStartTime" };
    private readonly Switch hasStart = new() { AutomationId = "SessionHasStart" };
    private readonly DatePicker endDate = DateControl();
    private readonly TimePicker endTime = new() { Format = "HH:mm", AutomationId = "SessionEndTime" };
    private readonly Microsoft.Maui.Controls.Entry duration = new() { Keyboard = Keyboard.Numeric, AutomationId = "SessionMinutes" };
    private readonly Microsoft.Maui.Controls.Entry estimate = new() { Keyboard = Keyboard.Numeric, Placeholder = "Opcional, em minutos", AutomationId = "SessionEstimatedMinutes" };
    private readonly Label comparison = new() { TextColor = Color.FromArgb("#203B34"), AutomationId = "SessionTimeComparison" };
    private readonly Label suggestionLabel = new() { TextColor = Color.FromArgb("#52645F") };
    private readonly Label error = new() { TextColor = Colors.DarkRed };
    private readonly Button useSuggestion = new() { Text = "Usar sugestão do planejamento", IsEnabled = false };
    private PlanTimeSuggestion suggestion = new(null, null);
    private bool syncing;
    private bool estimateEdited;

    public SessionTimeFields(StudySession session, bool isNew)
    {
        original = session; this.isNew = isNew;
        timing = new SessionTiming(session, isNew, DateTime.Now);
        startDate.AutomationId = "SessionStartDate"; endDate.AutomationId = "SessionEndDate";
        var stack = new VerticalStackLayout { Spacing = 10 };
        void Add(string title, View control) { stack.Add(new Label { Text = title, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#203B34") }); stack.Add(control); }
        Add("Data de início", startDate); Add("Informar horário de início", hasStart); Add("Horário de início", startTime);
        Add("Duração realizada (minutos)", duration);
        var shortcuts = new FlexLayout { Wrap = Microsoft.Maui.Layouts.FlexWrap.Wrap, JustifyContent = Microsoft.Maui.Layouts.FlexJustify.Start };
        foreach (var minutes in new[] { 30, 45, 50, 60, 90 })
        {
            var quick = new Button { Text = $"{minutes} min", AutomationId = $"SessionQuick{minutes}", Margin = new Thickness(0, 0, 8, 8) };
            quick.Command = new Command(() => Update(() => timing.ChangeMinutes(minutes)));
            shortcuts.Add(quick);
        }
        stack.Add(shortcuts);
        Add("Data de término", endDate); Add("Horário de término", endTime);
        stack.Add(new Label { Text = "Ao escolher um término anterior ao início no mesmo dia, a data avança para o dia seguinte. Para outros dias, ajuste a data de término.", TextColor = Color.FromArgb("#52645F") });
        Add("Duração estimada da sessão (minutos)", estimate);
        stack.Add(suggestionLabel);
        useSuggestion.Command = new Command(() => { if (suggestion.SuggestedMinutes is int value) { estimateEdited = true; estimate.Text = value.ToString(); } });
        stack.Add(useSuggestion); stack.Add(comparison);
        stack.Add(new Label { Text = "Comparação do tempo total da sessão. Mais ou menos tempo não indica desempenho, e não é dividido entre as lições.", TextColor = Color.FromArgb("#52645F") });
        stack.Add(error); Content = stack;
        estimate.Text = session.EstimatedMinutes?.ToString() ?? "";
        estimate.TextChanged += (_, _) => { if (!syncing) { estimateEdited = true; UpdateComparison(); } };
        duration.TextChanged += (_, _) =>
        {
            if (syncing) return;
            if (!int.TryParse(duration.Text, out var minutes) || minutes < 0) { error.Text = "Informe uma duração inteira, maior ou igual a zero."; return; }
            Update(() => timing.ChangeMinutes(minutes));
        };
        hasStart.Toggled += (_, e) => { if (!syncing) { var now = DateTime.Now; Update(() => timing.ChangeStart(timing.Date, e.Value ? new TimeOnly(now.Hour, now.Minute) : null)); } };
        startDate.DateSelected += (_, _) => { if (!syncing && startDate.Date is DateTime value) Update(() => timing.ChangeStart(DateOnly.FromDateTime(value), timing.Start)); };
        startTime.PropertyChanged += (_, e) => { if (!syncing && e.PropertyName == nameof(TimePicker.Time)) Update(() => timing.ChangeStart(timing.Date, startTime.Time is TimeSpan value ? TimeOnly.FromTimeSpan(value) : null)); };
        endDate.DateSelected += (_, _) => { if (!syncing && endDate.Date is DateTime value && timing.End is TimeOnly time) Update(() => timing.ChangeEnd(DateOnly.FromDateTime(value), time)); };
        endTime.PropertyChanged += (_, e) => { if (!syncing && e.PropertyName == nameof(TimePicker.Time) && endTime.Time is TimeSpan value) Update(() => timing.ChangeEndTime(TimeOnly.FromTimeSpan(value))); };
        Refresh();
    }

    private static DatePicker DateControl() => new() { Format = "dd/MM/yyyy", MinimumDate = new DateTime(1900, 1, 1), MaximumDate = DateTime.MaxValue.Date };
    private void Update(Action change)
    {
        try { change(); error.Text = ""; Refresh(); }
        catch (ArgumentException ex) { error.Text = ex.Message; }
    }
    private void Refresh()
    {
        syncing = true;
        try
        {
            startDate.Date = timing.Date.ToDateTime(TimeOnly.MinValue); startTime.Time = timing.Start?.ToTimeSpan();
            hasStart.IsToggled = timing.Start is not null; startTime.IsEnabled = hasStart.IsToggled;
            duration.Text = timing.Minutes.ToString();
            endDate.Date = timing.EndDate?.ToDateTime(TimeOnly.MinValue); endTime.Time = timing.End?.ToTimeSpan();
            endDate.IsEnabled = endTime.IsEnabled = timing.Start is not null;
        }
        finally { syncing = false; }
        UpdateComparison();
    }
    private int? Estimate()
    {
        if (string.IsNullOrWhiteSpace(estimate.Text)) return null;
        if (!int.TryParse(estimate.Text, out var value) || value < 0) throw new ArgumentException("Informe uma estimativa inteira, maior ou igual a zero, ou deixe em branco.");
        return value;
    }
    private void UpdateComparison()
    {
        try
        {
            var preview = original with { Minutes = timing.Minutes, EstimatedMinutes = Estimate(), PlannedMinutes = isNew ? suggestion.OriginalMinutes : original.PlannedMinutes };
            comparison.Text = SessionTimeComparison.Describe(preview);
        }
        catch (ArgumentException ex) { comparison.Text = ex.Message; }
    }
    public void SetSuggestion(PlanTimeSuggestion value)
    {
        suggestion = value;
        useSuggestion.IsEnabled = value.SuggestedMinutes is not null;
        suggestionLabel.Text = value.SuggestedMinutes is int minutes ? $"Sugestão do planejamento para todas as lições selecionadas: {minutes} min." : "Sem estimativa completa para as lições selecionadas. Informe o tempo total, se desejar.";
        if (isNew && original.EstimatedMinutes is null && !estimateEdited)
        {
            syncing = true; estimate.Text = value.SuggestedMinutes?.ToString() ?? ""; syncing = false;
        }
        UpdateComparison();
    }
    public void Apply(StudySession session)
    {
        if (!string.IsNullOrWhiteSpace(error.Text)) throw new ArgumentException(error.Text);
        if (!int.TryParse(duration.Text, out var minutes) || minutes < 0) throw new ArgumentException("Duração inválida.");
        timing.ChangeMinutes(minutes); timing.Apply(session); session.EstimatedMinutes = Estimate();
        SessionTiming.Validate(session);
    }
}

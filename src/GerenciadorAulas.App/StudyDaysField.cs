using GerenciadorAulas.Core;
namespace GerenciadorAulas.App;

public sealed class StudyDaysField : ContentView
{
    private readonly List<(DayOfWeek Day, CheckBox Check)> checks = [];
    public StudyDaysField(IEnumerable<DayOfWeek> selected)
    {
        var values = selected.ToHashSet();
        var stack = new VerticalStackLayout { Spacing = 0 };
        foreach (var (day, name) in StudyWeek.Options)
        {
            var check = new CheckBox { IsChecked = values.Contains(day), Color = Color.FromArgb("#14594E"), AutomationId = "StudyDay" + day };
            SemanticProperties.SetDescription(check, name);
            var label = new Label { Text = name, VerticalOptions = LayoutOptions.Center, TextColor = Color.FromArgb("#203B34"), FontSize = 16 };
            var row = new HorizontalStackLayout { MinimumHeightRequest = 48, Spacing = 8, Children = { check, label } };
            var tap = new TapGestureRecognizer(); tap.Tapped += (_, _) => check.IsChecked = !check.IsChecked; label.GestureRecognizers.Add(tap);
            checks.Add((day, check)); stack.Add(row);
        }
        Content = stack;
    }
    public DayOfWeek[] SelectedDays => StudyWeek.Normalize(checks.Where(item => item.Check.IsChecked).Select(item => item.Day));
}

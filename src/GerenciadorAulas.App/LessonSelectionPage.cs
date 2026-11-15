using CommunityToolkit.Mvvm.ComponentModel;
using GerenciadorAulas.Core;

namespace GerenciadorAulas.App;

public sealed class LessonSelectionPage : ContentPage
{
    private readonly LessonSelection selection;
    private readonly Dictionary<string, LessonRow> rows;
    private readonly SearchBar search = new() { Placeholder = "Pesquisar por número ou título" };
    private readonly Switch selectedOnly = new();
    private readonly Label summary = new() { TextColor = Color.FromArgb("#203B34"), FontSize = 14, MaxLines = 4 };
    private readonly CollectionView list = new() { SelectionMode = SelectionMode.None, EmptyView = "Nenhuma lição encontrada." };
    private readonly Button confirm = new();

    public LessonSelectionPage(LessonSelection selection, string subjectName, Action<LessonSelection> confirmed)
    {
        this.selection = selection;
        Title = "Selecionar lições";
        var selectedIds = selection.SelectedIds.ToHashSet();
        rows = selection.Options.ToDictionary(o => o.Id, o => new LessonRow(o, selectedIds.Contains(o.Id), Changed));
        var grid = new Grid
        {
            Padding = 16, RowSpacing = 10,
            RowDefinitions = { new(GridLength.Auto), new(GridLength.Auto), new(GridLength.Auto), new(GridLength.Auto), new(GridLength.Star), new(GridLength.Auto) }
        };
        grid.Add(new Label { Text = subjectName + " · marque uma ou várias lições", FontSize = 18, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#203B34") }, 0, 0);
        grid.Add(search, 0, 1);
        var filter = new HorizontalStackLayout { Spacing = 10, Children = { selectedOnly, new Label { Text = "Ver somente selecionadas", VerticalOptions = LayoutOptions.Center, TextColor = Color.FromArgb("#203B34") } } };
        SemanticProperties.SetDescription(selectedOnly, "Ver somente lições selecionadas");
        grid.Add(filter, 0, 2);
        grid.Add(summary, 0, 3);
        list.ItemTemplate = new DataTemplate(() =>
        {
            var row = new Grid { ColumnDefinitions = { new(new GridLength(48)), new(GridLength.Star) }, Padding = new Thickness(0, 8), MinimumHeightRequest = 68 };
            var check = new CheckBox { VerticalOptions = LayoutOptions.Center };
            check.SetBinding(CheckBox.IsCheckedProperty, nameof(LessonRow.IsSelected), BindingMode.TwoWay);
            check.SetBinding(SemanticProperties.DescriptionProperty, nameof(LessonRow.AccessibleName));
            var title = new Label { FontSize = 17, TextColor = Color.FromArgb("#203B34") };
            title.SetBinding(Label.TextProperty, nameof(LessonRow.Caption));
            var status = new Label { FontSize = 14, TextColor = Color.FromArgb("#52645F") };
            status.SetBinding(Label.TextProperty, nameof(LessonRow.Status));
            var text = new VerticalStackLayout { Spacing = 4, Children = { title, status } };
            var tap = new TapGestureRecognizer();
            tap.Tapped += (_, _) => { if (row.BindingContext is LessonRow item) item.IsSelected = !item.IsSelected; };
            text.GestureRecognizers.Add(tap);
            row.Add(check, 0); row.Add(text, 1);
            return row;
        });
        // A bounded Grid viewport lets the native CollectionView recycle its controls.
        grid.Add(list, 0, 4);
        var actions = new Grid { ColumnSpacing = 10, ColumnDefinitions = { new(GridLength.Star), new(GridLength.Star) } };
        var vm = new WorkspaceViewModel();
        confirm.Command = vm.Command(async () => { confirmed(selection); await Navigation.PopModalAsync(); });
        actions.Add(new Button { Text = "Cancelar", Command = vm.Command(async () => await Navigation.PopModalAsync()) }, 0);
        actions.Add(confirm, 1);
        grid.Add(actions, 0, 5);
        Content = grid;
        search.TextChanged += (_, _) => Refresh();
        selectedOnly.Toggled += (_, _) => Refresh();
        Refresh();
    }

    private void Changed(LessonRow row)
    {
        selection.Select(row.Id, row.IsSelected);
        UpdateSummary();
        if (selectedOnly.IsToggled) Dispatcher.Dispatch(Refresh);
    }

    private void UpdateSummary()
    {
        var chosen = selection.Selected;
        summary.Text = chosen.Count == 0 ? "Nenhuma selecionada. Lições concluídas também podem ser estudadas novamente." :
            $"{chosen.Count} selecionada(s): " + string.Join("; ", chosen.Take(3).Select(o => o.Caption)) +
            (chosen.Count > 3 ? ". Use “Ver somente selecionadas” para revisar todas." : "");
        confirm.Text = $"Usar {chosen.Count} lição(ões)";
    }

    private void Refresh()
    {
        list.ItemsSource = selection.Find(search.Text, selectedOnly.IsToggled).Select(o => rows[o.Id]).ToArray();
        UpdateSummary();
    }

    private sealed class LessonRow(LessonOption option, bool selected, Action<LessonRow> changed) : ObservableObject
    {
        private bool isSelected = selected;
        public string Id => option.Id;
        public string Caption => option.Caption;
        public string Status => option.Status;
        public string AccessibleName => Caption + " · " + Status;
        public bool IsSelected
        {
            get => isSelected;
            set { if (SetProperty(ref isSelected, value)) changed(this); }
        }
    }
}

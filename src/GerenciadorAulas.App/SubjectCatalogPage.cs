using GerenciadorAulas.Core;
namespace GerenciadorAulas.App;

public sealed class SubjectCatalogPage : ContentPage
{
    private readonly SubjectCatalogService catalogService;
    private readonly WorkspaceViewModel vm = new();
    private readonly VerticalStackLayout body = new() { Padding = 24, Spacing = 12, MaximumWidthRequest = 720, HorizontalOptions = LayoutOptions.Fill };
    private SubjectCatalog catalog = null!;
    private readonly string? yearId;
    private readonly DayOfWeek[] initialDays;
    private readonly Func<Task> saved;
    public SubjectCatalogPage(JournalStore store, string? yearId, DayOfWeek[] initialDays, Func<Task> saved)
    {
        catalogService = new(store); this.yearId = yearId; this.initialDays = initialDays; this.saved = saved;
        Title = yearId is null ? "Catálogo de matérias" : "Adicionar matérias do catálogo";
        BindingContext = vm; Content = new ScrollView { Content = body };
    }
    public async Task PrepareAsync()
    {
        catalog = await catalogService.GetAsync();
        if (yearId is null) BuildEditor(); else BuildImport();
    }
    private Label Note(string text) => new() { Text = text, TextColor = Color.FromArgb("#394F48"), FontSize = 16 };
    private Button Action(string text, Func<Task> action) => new() { Text = text, Command = vm.Command(action) };
    private void Footer()
    {
        var error = new Label { TextColor = Color.FromArgb("#9C1C1C") }; error.SetBinding(Label.TextProperty, nameof(vm.Error)); body.Add(error);
        body.Add(Action("Cancelar", async () => await Navigation.PopModalAsync()));
    }
    private void BuildEditor()
    {
        body.Clear(); body.Add(Note("Edite os nomes e a ordem do catálogo. As alterações não renomeiam nem removem matérias dos alunos. Salve para confirmar."));
        var list = new VerticalStackLayout { Spacing = 12 }; body.Add(list);
        void Populate()
        {
            list.Clear();
            foreach (var item in catalog.Items.ToArray())
            {
                var name = new Microsoft.Maui.Controls.Entry { Text = item.Name, Placeholder = "Nome da matéria", FontSize = 17 };
                SemanticProperties.SetDescription(name, "Nome da matéria no catálogo");
                name.TextChanged += (_, _) => { var index = catalog.Items.FindIndex(entry => entry.Id == item.Id); if (index >= 0) catalog.Items[index] = catalog.Items[index] with { Name = name.Text ?? "" }; };
                var up = Action("Subir", () => { Move(item.Id, -1); Populate(); return Task.CompletedTask; });
                var down = Action("Descer", () => { Move(item.Id, 1); Populate(); return Task.CompletedTask; });
                up.IsEnabled = catalog.Items[0].Id != item.Id; down.IsEnabled = catalog.Items[^1].Id != item.Id;
                list.Add(new Border { BackgroundColor = Colors.White, Padding = 12, Stroke = Color.FromArgb("#CCD8D1"), Content = new VerticalStackLayout { Spacing = 8, Children = {
                    name, new FlexLayout { Wrap = Microsoft.Maui.Layouts.FlexWrap.Wrap, Children = { up, down, Action("Remover do catálogo", () => { catalog.Items.RemoveAll(entry => entry.Id == item.Id); Populate(); return Task.CompletedTask; }) } }
                } } });
            }
        }
        Populate();
        var custom = new Microsoft.Maui.Controls.Entry { Placeholder = "Nova matéria personalizada" }; body.Add(custom);
        body.Add(Action("Adicionar ao catálogo", () =>
        {
            if (string.IsNullOrWhiteSpace(custom.Text)) throw new ArgumentException("Informe o nome da matéria.");
            if (catalog.Items.Any(item => SubjectCatalogService.NameKey(item.Name) == SubjectCatalogService.NameKey(custom.Text))) throw new ArgumentException("Essa matéria já está no catálogo.");
            catalog.Items.Add(new(Guid.NewGuid().ToString("N"), custom.Text.Trim())); custom.Text = ""; Populate(); return Task.CompletedTask;
        }));
        body.Add(Action("Salvar catálogo", async () => { await catalogService.SaveAsync(catalog); await saved(); await Navigation.PopModalAsync(); })); Footer();
    }
    private void Move(string id, int offset)
    {
        var index = catalog.Items.FindIndex(item => item.Id == id); var target = index + offset;
        if (index < 0 || target < 0 || target >= catalog.Items.Count) return;
        (catalog.Items[index], catalog.Items[target]) = (catalog.Items[target], catalog.Items[index]);
    }
    private void BuildImport()
    {
        body.Add(Note("Selecione as matérias e configure cada uma. Nenhuma será adicionada sem confirmação. Matérias já existentes serão preservadas e ignoradas na importação."));
        var drafts = new List<Func<SubjectImport?>>();
        foreach (var item in catalog.Items)
        {
            var check = new CheckBox { Color = Color.FromArgb("#14594E") }; SemanticProperties.SetDescription(check, "Adicionar " + item.Name);
            var label = Note(item.Name); label.FontAttributes = FontAttributes.Bold; label.VerticalOptions = LayoutOptions.Center;
            var tap = new TapGestureRecognizer(); tap.Tapped += (_, _) => check.IsChecked = !check.IsChecked; label.GestureRecognizers.Add(tap);
            var count = new Microsoft.Maui.Controls.Entry { Placeholder = "Quantidade de lições (1 a 5000)", Keyboard = Keyboard.Numeric };
            var pace = new Microsoft.Maui.Controls.Entry { Text = "1", Keyboard = Keyboard.Numeric };
            var days = new StudyDaysField(initialDays);
            var configuration = new VerticalStackLayout { IsVisible = false, Spacing = 8, Children = { Note("Quantidade de lições"), count, Note("Lições por dia selecionado"), pace, Note("Dias desta matéria (respeitam também o calendário do ano)"), days } };
            check.CheckedChanged += (_, _) => configuration.IsVisible = check.IsChecked;
            body.Add(new Border { BackgroundColor = Colors.White, Stroke = Color.FromArgb("#CCD8D1"), Padding = 12, Content = new VerticalStackLayout { Spacing = 8, Children = { new HorizontalStackLayout { MinimumHeightRequest = 48, Spacing = 8, Children = { check, label } }, configuration } } });
            drafts.Add(() =>
            {
                if (!check.IsChecked) return null;
                if (!int.TryParse(count.Text, out var total) || !int.TryParse(pace.Text, out var perDay)) throw new ArgumentException(item.Name + ": informe a quantidade e o ritmo em números inteiros.");
                return new(item.Id, total, perDay, days.SelectedDays);
            });
        }
        if (catalog.Items.Count == 0) body.Add(Note("O catálogo está vazio. Adicione matérias nas Configurações ou crie uma matéria personalizada neste ano."));
        body.Add(Action("Adicionar matérias selecionadas", async () =>
        {
            var result = await catalogService.ImportAsync(yearId!, drafts.Select(read => read()).OfType<SubjectImport>().ToArray());
            await DisplayAlertAsync("Matérias", $"{result.Added} adicionada(s); {result.Skipped} já existente(s), preservada(s).", "OK");
            await saved(); await Navigation.PopModalAsync();
        })); Footer();
    }
}

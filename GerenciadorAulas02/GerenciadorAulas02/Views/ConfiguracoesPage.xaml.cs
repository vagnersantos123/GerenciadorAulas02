using Microsoft.Maui.Controls;
using GerenciadorAulas02.ViewModels;
using GerenciadorAulas02.Models;

namespace GerenciadorAulas02.Views;

public partial class ConfiguracoesPage : ContentPage
{
    public ConfiguracoesPage()
    {
        InitializeComponent();
        BindingContext = new ConfiguracoesViewModel();
    }
    private async void EntryDuracao_Unfocused(object sender, FocusEventArgs e)
    {
        if (sender is Entry entry && entry.BindingContext is Materia materia)
        {
            await App.Database.SaveMateriaAsync(materia);
        }
    }
    private async void StepperDuracao_ValueChanged(object sender, ValueChangedEventArgs e)
    {
        if (sender is Stepper stepper && stepper.BindingContext is Materia materia)
        {
            // Atualiza o valor da matéria
            materia.Duracao = (int)e.NewValue;

            // Salva imediatamente no banco
            await App.Database.SaveMateriaAsync(materia);
        }
    }


}

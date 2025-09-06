using Microsoft.Maui.Controls;
using GerenciadorAulas02.ViewModels;

namespace GerenciadorAulas02.Views;

public partial class AlunosPage : ContentPage
{
    public AlunosPage()
    {
        InitializeComponent();
        BindingContext = new AlunosViewModel(); // ✅ não recebe aula
    }
}

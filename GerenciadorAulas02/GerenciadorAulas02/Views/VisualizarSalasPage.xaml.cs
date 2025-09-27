using Microsoft.Maui.Controls;
using GerenciadorAulas02.Services;

namespace GerenciadorAulas02.Views;

public partial class VisualizarSalasPage : ContentPage
{
    private readonly AulaDatabase database;

    public VisualizarSalasPage(AulaDatabase database)
    {
        InitializeComponent();
        this.database = database;
    }
}

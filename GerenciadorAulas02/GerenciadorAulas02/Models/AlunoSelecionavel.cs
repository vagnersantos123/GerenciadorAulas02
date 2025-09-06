using System.ComponentModel;

namespace GerenciadorAulas02.Models;

public class AlunoSelecionavel : INotifyPropertyChanged
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    private bool isSelecionado;
    public bool IsSelecionado
    {
        get => isSelecionado;
        set
        {
            if (isSelecionado != value)
            {
                isSelecionado = value;
                OnPropertyChanged(nameof(IsSelecionado));
            }
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected void OnPropertyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

using System.ComponentModel;

namespace GerenciadorAulas02.Models;

public class Aula : INotifyPropertyChanged
{
    private int id;
    public int Id
    {
        get => id;
        set { id = value; OnPropertyChanged(nameof(Id)); }
    }

    private string titulo = string.Empty;
    public string Titulo
    {
        get => titulo;
        set { titulo = value; OnPropertyChanged(nameof(Titulo)); }
    }

    private string descricao = string.Empty;
    public string Descricao
    {
        get => descricao;
        set { descricao = value; OnPropertyChanged(nameof(Descricao)); }
    }

    private DateTime data;
    public DateTime Data
    {
        get => data;
        set { data = value; OnPropertyChanged(nameof(Data)); }
    }

    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged(string nome)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nome));
    }
}

using SQLite;
using System.ComponentModel;

namespace GerenciadorAulas02.Models;

public class SalaDeAula : INotifyPropertyChanged
{
    private int id;
    private string nome = string.Empty;
    private string descricao = string.Empty;

    [PrimaryKey, AutoIncrement]
    public int Id
    {
        get => id;
        set { id = value; OnPropertyChanged(nameof(Id)); }
    }

    public string Nome
    {
        get => nome;
        set { nome = value; OnPropertyChanged(nameof(Nome)); }
    }

    // ← nova propriedade
    public string Descricao
    {
        get => descricao;
        set { descricao = value; OnPropertyChanged(nameof(Descricao)); }
    }

    [Ignore]
    public List<Aula> Aulas { get; set; } = new();

    public override string ToString() => Nome;

    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged(string propName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
}

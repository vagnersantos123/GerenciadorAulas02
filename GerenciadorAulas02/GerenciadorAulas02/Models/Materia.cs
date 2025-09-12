using SQLite;
using System.ComponentModel;

namespace GerenciadorAulas02.Models;

public class Materia : INotifyPropertyChanged
{
    private int id;
    private string nome = string.Empty;

    [PrimaryKey, AutoIncrement]
    public int Id
    {
        get => id;
        set { id = value; OnPropertyChanged(nameof(Id)); }
    }

    [NotNull]
    public string Nome
    {
        get => nome;
        set { nome = value; OnPropertyChanged(nameof(Nome)); }
    }

    public override string ToString() => Nome;

    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged(string propName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
}

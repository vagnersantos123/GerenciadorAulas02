using SQLite;
using System.ComponentModel;
using GerenciadorAulas02.Services;

namespace GerenciadorAulas02.Models;

public class Materia : INotifyPropertyChanged
{
    private int id;
    private string nome = string.Empty;

    // Adicionadno uma duracao a materia
    private int? duracao; // duração em minutos

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

    public int Duracao
    {
        get => duracao ?? PreferenciasGlobais.DuracaoPadrao;
        set { duracao = value; OnPropertyChanged(nameof(Duracao)); }
    }   

    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

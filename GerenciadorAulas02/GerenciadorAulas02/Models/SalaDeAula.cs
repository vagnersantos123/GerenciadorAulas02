using SQLite;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace GerenciadorAulas02.Models;

public class SalaDeAula : INotifyPropertyChanged
{
    private int id;
    private string nome = string.Empty;
    private string descricao = string.Empty;

    // Novas propriedades
    private DateTime dataInicioAnoLetivo = DateTime.Today;
    private DateTime dataFimAnoLetivo = DateTime.Today.AddMonths(10);

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

    public string Descricao
    {
        get => descricao;
        set { descricao = value; OnPropertyChanged(nameof(Descricao)); }
    }

    public DateTime DataInicioAnoLetivo
    {
        get => dataInicioAnoLetivo;
        set => SetProperty(ref dataInicioAnoLetivo, value);
    }

    public DateTime DataFimAnoLetivo
    {
        get => dataFimAnoLetivo;
        set => SetProperty(ref dataFimAnoLetivo, value);
    }
    protected bool SetProperty<T>(ref T backingStore, T value, string propertyName = null!)
    {
        if (EqualityComparer<T>.Default.Equals(backingStore, value))
            return false;

        backingStore = value;
        OnPropertyChanged(propertyName ?? string.Empty);
        return true;
    }

    [Ignore]
    public List<Aula> Aulas { get; set; } = new();

    public override string ToString() => Nome;

    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged(string propName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
}

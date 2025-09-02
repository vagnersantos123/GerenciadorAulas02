using SQLite;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace GerenciadorAulas02.Models;

public class Aula : INotifyPropertyChanged
{

    private int id;

    [PrimaryKey, AutoIncrement] 
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

    private TimeSpan duracao;
    public TimeSpan Duracao
    {
        get => duracao;
        set { duracao = value; OnPropertyChanged(nameof(Duracao)); }
    }

    private string tipo = "Teórica";
    public string Tipo
    {
        get => tipo;
        set { tipo = value; OnPropertyChanged(nameof(Tipo)); }
    }

    [Ignore]
    public List<string> AlunosPresentes { get; set; } = new List<string>();

    // Método de resumo da aula
    public string Resumo => $"{Titulo} ({Tipo}) - {Data:dd/MM/yyyy HH:mm}, {Duracao.TotalMinutes} min, {AlunosPresentes.Count} alunos";

    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged(string nome)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nome));
    }
}

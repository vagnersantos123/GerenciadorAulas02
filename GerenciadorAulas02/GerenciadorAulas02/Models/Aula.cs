using SQLite;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace GerenciadorAulas02.Models;

public class Aula : INotifyPropertyChanged
{
    private int id;
    private string titulo = string.Empty;
    private string descricao = string.Empty;
    private DateTime data;
    private TimeSpan duracao;
    private string tipo = "Teórica";
    private int? salaDeAulaId; // 🔹 agora é opcional (nullable)

    [PrimaryKey, AutoIncrement]
    public int Id
    {
        get => id;
        set { id = value; OnPropertyChanged(nameof(Id)); }
    }

    public string Titulo
    {
        get => titulo;
        set { titulo = value; OnPropertyChanged(nameof(Titulo)); }
    }

    public string Descricao
    {
        get => descricao;
        set { descricao = value; OnPropertyChanged(nameof(Descricao)); }
    }

    public DateTime Data
    {
        get => data;
        set { data = value; OnPropertyChanged(nameof(Data)); }
    }

    public TimeSpan Duracao
    {
        get => duracao;
        set { duracao = value; OnPropertyChanged(nameof(Duracao)); }
    }

    public string Tipo
    {
        get => tipo;
        set { tipo = value; OnPropertyChanged(nameof(Tipo)); }
    }

    // 🔹 Chave estrangeira para SalaDeAula (opcional)
    public int? SalaDeAulaId
    {
        get => salaDeAulaId;
        set { salaDeAulaId = value; OnPropertyChanged(nameof(SalaDeAulaId)); }
    }

    [Ignore]
    public List<string> AlunosPresentes { get; set; } = new List<string>();

    // 🔹 Resumo automático
    [Ignore]
    public string Resumo => $"{Titulo} ({Tipo}) - {Data:dd/MM/yyyy HH:mm}, {Duracao.TotalMinutes} min, {AlunosPresentes.Count} alunos";

    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged(string nome)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nome));
    }
}

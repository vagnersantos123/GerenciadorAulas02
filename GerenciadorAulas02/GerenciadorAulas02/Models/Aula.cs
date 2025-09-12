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
    private int? salaDeAulaId;   // <- aqui a FK
    private int? materiaId;      // FK para matéria (se tiver)

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

    // Chave estrangeira para SalaDeAula (nullable)
    public int? SalaDeAulaId
    {
        get => salaDeAulaId;
        set { salaDeAulaId = value; OnPropertyChanged(nameof(SalaDeAulaId)); }
    }

    // Chave estrangeira para Materia (nullable)
    public int? MateriaId
    {
        get => materiaId;
        set { materiaId = value; OnPropertyChanged(nameof(MateriaId)); }
    }

    // Propriedades não persistidas (apenas para navegação / UI)
    [Ignore]
    public Materia? Materia { get; set; }

    [Ignore]
    public List<string> AlunosPresentes { get; set; } = new List<string>();

    [Ignore]
    public string Resumo => $"{(Materia != null ? Materia.Nome : Titulo)} ({Tipo}) - {Data:dd/MM/yyyy HH:mm}, {Duracao.TotalMinutes} min";

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged(string nome) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nome));
}

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
    private TimeSpan duracao;
    private string tipo = "Teórica";

    private int? salaDeAulaId;
    private int? materiaId;
    private Materia? materia;

    private DateTime inicio;
    private DateTime fim;
    private DateTime diaAula;

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

    public int? SalaDeAulaId
    {
        get => salaDeAulaId ?? 0;
        set { salaDeAulaId = value; OnPropertyChanged(nameof(SalaDeAulaId)); }
    }

    public int? MateriaId
    {
        get => materiaId ?? 0;
        set { materiaId = value; OnPropertyChanged(nameof(MateriaId)); }
    }

    public DateTime DiaAula
    {
        get => diaAula;
        set { diaAula = value; OnPropertyChanged(nameof(DiaAula)); }
    }

    public string DiaAulaFormatado => DiaAula.ToString("dd/MM/yyyy");


    [Ignore]
    public Materia? Materia
    {
        get => materia;
        set
        {
            materia = value;
            OnPropertyChanged(nameof(Materia));

            if (materia != null && duracao == default)
            {
                Duracao = TimeSpan.FromMinutes(materia.Duracao);
                Fim = Inicio.Add(Duracao);
            }
        }
    }

    [Ignore]
    public List<string> AlunosPresentes { get; set; } = new List<string>();

    public DateTime Inicio
    {
        get => inicio;
        set
        {
            inicio = value;
            OnPropertyChanged(nameof(Inicio));

            // Atualiza fim automaticamente se a duração já estiver definida
            if (duracao != default)
                fim = inicio.Add(duracao);
            OnPropertyChanged(nameof(Fim));
        }
    }

    public DateTime Fim
    {
        get => fim;
        set
        {
            fim = value;
            OnPropertyChanged(nameof(Fim));

            // Atualiza duração com base no início e fim
            duracao = fim - inicio;
            OnPropertyChanged(nameof(Duracao));
        }
    }

    [Ignore]
    public string Resumo
    {
        get
        {
            string materiaNome = Materia != null ? Materia.Nome : "Sem matéria";
            return $"{Titulo} - {materiaNome} ({Tipo}) - {Inicio:dd/MM/yyyy HH:mm} até {Fim:HH:mm}, {Duracao.TotalMinutes} min, {AlunosPresentes.Count} alunos";
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged(string nome)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nome));
    }
}

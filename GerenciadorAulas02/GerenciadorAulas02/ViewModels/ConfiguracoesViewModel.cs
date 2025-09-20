using GerenciadorAulas02.Models;
using GerenciadorAulas02.Services;
using Microsoft.Maui.Controls;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace GerenciadorAulas02.ViewModels;

public class ConfiguracoesViewModel : INotifyPropertyChanged
{
    // ================= DATAS =================
    public DateTime DataInicioAno
    {
        get => PreferenciasGlobais.DataInicioAno;
        set
        {
            if (PreferenciasGlobais.DataInicioAno != value)
            {
                PreferenciasGlobais.DataInicioAno = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DataFimAno));
            }
        }
    }

    public DateTime DataFimAno => PreferenciasGlobais.DataFimAno;

    // ================= PREFERÊNCIAS =================
    public int DuracaoPadrao
    {
        get => PreferenciasGlobais.DuracaoPadrao;
        set => PreferenciasGlobais.DuracaoPadrao = value;
    }

    public int QuantidadeAulasPadrao
    {
        get => PreferenciasGlobais.QuantidadeAulasPadrao;
        set => PreferenciasGlobais.QuantidadeAulasPadrao = value;
    }

    public int IntervaloDiasPadrao
    {
        get => PreferenciasGlobais.IntervaloDiasPadrao;
        set => PreferenciasGlobais.IntervaloDiasPadrao = value;
    }

    public bool TemaEscuro
    {
        get => PreferenciasGlobais.TemaEscuro;
        set
        {
            PreferenciasGlobais.TemaEscuro = value;
            Application.Current.UserAppTheme = value ? AppTheme.Dark : AppTheme.Light;
        }
    }

    // ================= EVENTO =================
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string nome = "")
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nome));
}

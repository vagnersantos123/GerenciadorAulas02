using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using GerenciadorAulas02.Models;
using GerenciadorAulas02.Services;
using GerenciadorAulas02.Helpers;
using GerenciadorAulas02.Views;

namespace GerenciadorAulas02.ViewModels
{
    public class SalasViewModel : BaseViewModelPreferencias
    {
        public ObservableCollection<SalaDeAula> Salas { get; } = new();

        private string nomeSala;
        public string NomeSala
        {
            get => nomeSala;
            set => SetProperty(ref nomeSala, value);
        }

        private DateTime dataInicioAno;
        public DateTime DataInicioAno
        {
            get => dataInicioAno;
            set
            {
                if (SetProperty(ref dataInicioAno, value))
                {
                    // Atualiza o DataFimAno automaticamente
                    dataFimAno = dataInicioAno.AddMonths(10);
                    OnPropertyChanged(nameof(DataFimAno));

                    // Salva nas PreferenciasGlobais
                    PreferenciasGlobais.DataInicioAno = dataInicioAno;
                    // PreferenciasGlobais.DataFimAno é atualizada automaticamente pelo setter
                }
            }
        }

        private DateTime dataFimAno;
        public DateTime DataFimAno
        {
            get => dataFimAno;
            set => SetProperty(ref dataFimAno, value);
        }

        public ICommand AdicionarSalaCommand { get; }
        public ICommand ExcluirSalaCommand { get; }
        public ICommand SelecionarSalaCommand { get; }

        public SalasViewModel()
        {
            AdicionarSalaCommand = new Command(async () => await AdicionarSala());
            ExcluirSalaCommand = new Command<SalaDeAula>(async (s) => await ExcluirSala(s));
            SelecionarSalaCommand = new Command<SalaDeAula>(async (s) => await AbrirSala(s));

            // Inicializa datas com PreferenciasGlobais
            dataInicioAno = PreferenciasGlobais.DataInicioAno;
            dataFimAno = PreferenciasGlobais.DataFimAno;

            _ = LoadSalas();
        }

        private async Task LoadSalas()
        {
            var salas = await App.Database.GetSalasAsync();
            Salas.Clear();
            foreach (var s in salas) Salas.Add(s);
        }

        private async Task AdicionarSala()
        {
            if (string.IsNullOrWhiteSpace(NomeSala))
            {
                await Application.Current.MainPage.DisplayAlert("Erro", "Digite um nome para a sala", "OK");
                return;
            }

            var novaSala = new SalaDeAula
            {
                Nome = NomeSala,
                DataInicioAnoLetivo = DataInicioAno,
                DataFimAnoLetivo = DataFimAno
            };

            await App.Database.SaveSalaAsync(novaSala);

            NomeSala = string.Empty;

            // Reset para valores atuais das preferências
            AtualizarPreferenciasGlobais();

            await LoadSalas();
        }

        private async Task ExcluirSala(SalaDeAula sala)
        {
            if (sala == null) return;

            bool confirmar = await Application.Current.MainPage.DisplayAlert(
                "Confirmar",
                $"Deseja excluir a sala {sala.Nome}?",
                "Sim", "Não");

            if (!confirmar) return;

            await App.Database.DeleteSalaAsync(sala);
            await LoadSalas();
        }

        private async Task AbrirSala(SalaDeAula sala)
        {
            if (sala == null) return;

            // Navega para a página de aulas, passando a sala
            await Application.Current.MainPage.Navigation.PushAsync(new AulasPage(sala));
        }
    }
}

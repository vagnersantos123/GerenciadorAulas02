using System.ComponentModel;
using System.Windows.Input;
using Microsoft.Maui.Controls;

namespace GerenciadorAulas02.ViewModels
{
    public class LoginViewModel : INotifyPropertyChanged
    {
        private string usuario;
        public string Usuario
        {
            get => usuario;
            set
            {
                usuario = value;
                OnPropertyChanged(nameof(Usuario));
            }
        }

        private string senha;
        public string Senha
        {
            get => senha;
            set
            {
                senha = value;
                OnPropertyChanged(nameof(Senha));
            }
        }

        public ICommand EntrarCommand { get; }

        public LoginViewModel()
        {
            EntrarCommand = new Command(OnEntrar);
        }

        private async void OnEntrar()
        {
            if (Usuario == "admin" && Senha == "1234")
            {
                // Navega para HomePage
                await Application.Current.MainPage.Navigation.PushAsync(new Views.HomePage());
            }
            else
            {
                await Application.Current.MainPage.DisplayAlert("Erro", "Usuário ou senha incorretos", "OK");
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string nome)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nome));
        }
    }
}

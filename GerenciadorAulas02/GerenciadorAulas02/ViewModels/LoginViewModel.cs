using System.ComponentModel;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage; // Para Preferences

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

        private bool manterConectado;
        public bool ManterConectado
        {
            get => manterConectado;
            set
            {
                manterConectado = value;
                OnPropertyChanged(nameof(ManterConectado));
            }
        }

        public ICommand EntrarCommand { get; }

        public LoginViewModel()
        {
            EntrarCommand = new Command(OnEntrar);

            // Lê a preferência ao iniciar a ViewModel
            ManterConectado = Preferences.Get("ManterConectado", false);
        }

        private async void OnEntrar()
        {
            // Simulação de login
            if (Usuario == "admin" && Senha == "1234")
            {
                // Salva preferência se estiver marcado
                Preferences.Set("ManterConectado", ManterConectado);
                if (ManterConectado)
                {
                    Preferences.Set("UsuarioLogado", Usuario); // opcional
                }
                else
                {
                    Preferences.Remove("UsuarioLogado");
                }

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

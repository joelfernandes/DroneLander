using DroneLander.Models;
using DroneLander.ViewModels;
using Xamarin.Forms;

namespace DroneLander
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            if (App.ViewModel == null) App.ViewModel = new MainViewModel(this);

            BindingContext = App.ViewModel;

            MessagingCenter.Subscribe<MainPage, LandingResultType>(this, "ActivityUpdate", (sender, arg) =>
            {
                string title = arg.ToString();
                string message = (arg == LandingResultType.Landed) ? "The Eagle has landed!" : "That's going to leave a mark!";
                if (arg == LandingResultType.Kaboom) App.ViewModel.ShakeLandscapeAsync(this);

                Device.BeginInvokeOnMainThread(() =>
                {
                    DisplayAlert(title, message, "OK");
                    App.ViewModel.ResetLanding();
                });
            });
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            MessagingCenter.Unsubscribe<MainPage, LandingResultType>(this, "ActivityUpdate");
        }
    }
}
using System;
using System.Threading.Tasks;
using System.Windows.Input;
using DroneLander.Common;
using DroneLander.Models;
using Xamarin.Forms;

namespace DroneLander.ViewModels
{
    public class MainViewModel : ObservableBase
    {
        public MainViewModel(MainPage activityPage)
        {
            ActivityPage = activityPage;
            ActiveLandingParameters = new LandingParameters();
            Altitude = ActiveLandingParameters.Altitude;
            Velocity = ActiveLandingParameters.Velocity;
            Fuel = ActiveLandingParameters.Fuel;
            Thrust = ActiveLandingParameters.Thrust;
            FuelRemaining = CoreConstants.StartingFuel;
            IsActive = false;
        }

        private MainPage _activityPage;
        public MainPage ActivityPage
        {
            get { return _activityPage; }
            set { SetProperty(ref _activityPage, value); }
        }

        public LandingParameters ActiveLandingParameters { get; set; }

        private double _altitude;
        public double Altitude
        {
            get { return _altitude; }
            set { SetProperty(ref _altitude, value); }
        }

        private double _descentRate;
        public double DescentRate
        {
            get { return _descentRate; }
            set { SetProperty(ref _descentRate, value); }
        }

        private double _velocity;
        public double Velocity
        {
            get { return _velocity; }
            set { SetProperty(ref _velocity, value); }
        }

        private double _fuel;
        public double Fuel
        {
            get { return _fuel; }
            set { SetProperty(ref _fuel, value); }
        }

        private double _fuelRemaining;
        public double FuelRemaining
        {
            get { return _fuelRemaining; }
            set { SetProperty(ref _fuelRemaining, value); }
        }

        private double _thrust;
        public double Thrust
        {
            get { return _thrust; }
            set { SetProperty(ref _thrust, value); }
        }

        private double _throttle;
        public double Throttle
        {
            get { return this._throttle; }
            set
            {
                this.SetProperty(ref this._throttle, value);
                if (this.IsActive && this.FuelRemaining > 0.0) Helpers.AudioHelper.AdjustVolume(value);
            }
        }

        private bool _isActionable() => true;
        private string _actionLabel;
        public string ActionLabel
        {
            get { return _actionLabel; }
            set { SetProperty(ref _actionLabel, value); }
        }

        private bool _isActive;
        public bool IsActive
        {
            get { return _isActive; }
            set { SetProperty(ref _isActive, value); ActionLabel = (IsActive) ? "Reset" : "Start"; }
        }

        public ICommand AttemptLandingCommand
        {
            get
            {
                return new RelayCommand(() =>
                {
                    IsActive = !IsActive;

                    if (IsActive)
                    {
                        StartLanding();
                    }
                    else
                    {
                        ResetLanding();
                    }

                }, _isActionable);
            }
        }

        public void StartLanding()
        {
            Helpers.AudioHelper.ToggleEngine();

            Device.StartTimer(TimeSpan.FromMilliseconds(Common.CoreConstants.PollingIncrement), () =>
            {
                UpdateFlightParameters();

                if (this.ActiveLandingParameters.Altitude > 0.0)
                {
                    Device.BeginInvokeOnMainThread(() =>
                    {
                        this.Altitude = this.ActiveLandingParameters.Altitude;
                        this.DescentRate = this.ActiveLandingParameters.Velocity;
                        this.FuelRemaining = this.ActiveLandingParameters.Fuel / 1000;
                        this.Thrust = this.ActiveLandingParameters.Thrust;
                    });

                    if (this.FuelRemaining == 0.0) Helpers.AudioHelper.KillEngine();

                    return this.IsActive;
                }
                else
                {
                    this.ActiveLandingParameters.Altitude = 0.0;
                    this.IsActive = false;

                    Device.BeginInvokeOnMainThread(() =>
                    {
                        this.Altitude = this.ActiveLandingParameters.Altitude;
                        this.DescentRate = this.ActiveLandingParameters.Velocity;
                        this.FuelRemaining = this.ActiveLandingParameters.Fuel / 1000;
                        this.Thrust = this.ActiveLandingParameters.Thrust;
                    });

                    if (this.ActiveLandingParameters.Velocity > -5.0)
                    {
                        MessagingCenter.Send(this.ActivityPage, "ActivityUpdate", LandingResultType.Landed);
                    }
                    else
                    {
                        MessagingCenter.Send(this.ActivityPage, "ActivityUpdate", LandingResultType.Kaboom);
                    }

                    return false;
                }
            });
        }

        private void UpdateFlightParameters()
        {
            double seconds = CoreConstants.PollingIncrement / 1000.0;

            // Compute thrust and remaining fuel
            //thrust = throttle * 1200.0;
            var used = (Throttle * seconds) / 10.0;
            used = Math.Min(used, ActiveLandingParameters.Fuel); // Can't burn more fuel than you have
            ActiveLandingParameters.Thrust = used * 25000.0;
            ActiveLandingParameters.Fuel -= used;

            // Compute new flight parameters
            double avgmass = CoreConstants.LanderMass + (used / 2.0);
            double force = ActiveLandingParameters.Thrust - (avgmass * CoreConstants.Gravity);
            double acc = force / avgmass;

            double vel2 = ActiveLandingParameters.Velocity + (acc * seconds);
            double avgvel = (ActiveLandingParameters.Velocity + vel2) / 2.0;
            ActiveLandingParameters.Altitude += (avgvel * seconds);
            ActiveLandingParameters.Velocity = vel2;
        }

        public async void ShakeLandscapeAsync(ContentPage page)
        {
            try
            {
                for (int i = 0; i < 8; i++)
                {
                    await Task.WhenAll(
                        page.ScaleTo(1.1, 20, Easing.Linear),
                        page.TranslateTo(-30, 0, 20, Easing.Linear)
                    );

                    await Task.WhenAll(
                        page.TranslateTo(0, 0, 20, Easing.Linear)
                    );

                    await Task.WhenAll(
                        page.TranslateTo(0, -30, 20, Easing.Linear)
                    );

                    await Task.WhenAll(
                        page.ScaleTo(1.0, 20, Easing.Linear),
                        page.TranslateTo(0, 0, 20, Easing.Linear)
                    );
                }
            }
            catch { }
        }

        public async void ResetLanding()
        {
            Helpers.AudioHelper.ToggleEngine();

            await Task.Delay(500);

            ActiveLandingParameters = new LandingParameters();

            Altitude = 5000.0;
            Velocity = 0.0;
            Fuel = 1000.0;
            FuelRemaining = 1000.0;
            Thrust = 0.0;
            DescentRate = 0.0;
            Throttle = 0.0;
        }
    }
}
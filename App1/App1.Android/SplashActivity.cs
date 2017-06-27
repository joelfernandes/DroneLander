﻿using Android.App;
using Android.Content;
using System.Threading.Tasks;

namespace DroneLander.Droid
{
    [Activity(Label = "Drone Lander", Theme = "@style/Theme.Splash", MainLauncher = true, NoHistory = true)]
    public class SplashActivity : Activity
    {
        protected override void OnResume()
        {
            base.OnResume();

            Task startupWork = new Task(async () =>
            {
                await Task.Delay(300);
            });

            startupWork.ContinueWith(t =>
            {
                StartActivity(new Intent(Application.Context, typeof(MainActivity)));
            }, TaskScheduler.FromCurrentSynchronizationContext());

            startupWork.Start();
        }
    }
}
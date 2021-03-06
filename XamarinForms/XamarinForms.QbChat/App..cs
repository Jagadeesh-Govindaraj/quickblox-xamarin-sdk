﻿using System;
using System.Linq;
using XamarinForms.QbChat.Pages;
using XamarinForms.QbChat.Repository;
using Xamarin.Forms;
using Quickblox.Sdk.Modules.ChatXmppModule;

namespace XamarinForms.QbChat
{
    public class App : Application
    {
        public static QbProvider QbProvider { get; set; }
        public static INavigation Navigation { get; set; }
        public static Action<string> LogConsole;

		public static int UserLogin {
			get;
			set;
		}

		public static string UserPassword {
			get;
			set;
		}
        
		public static string Version {
			get;
			set;
		}

        public App()
        {
            QbProvider = new QbProvider();
			SetLoginPage();
        }

        protected override async void OnStart()
        {
            base.OnStart();
        }

        protected override void OnSleep()
        {
            base.OnSleep();
        }

        protected override void OnResume()
        {
            base.OnResume();
        }

		public static void SetLoginPage()
		{
			((App)App.Current).MainPage = new NavigationPage(new LoginPage());
		}

		public static void SetMainPage()
        {
			var page = new NavigationPage(new ChatsPage());
			Navigation = page.Navigation;
			((App)App.Current).MainPage = page;
        }
    }
}

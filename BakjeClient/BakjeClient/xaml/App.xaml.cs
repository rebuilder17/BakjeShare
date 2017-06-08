using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Xamarin.Forms;

namespace BakjeClient
{
	public partial class App : Application
	{
		public static App instance { get; private set; }
		//public INavigation navigation { get; private set; }
		//NavigationPage	m_navPage;

		public App()
		{
			instance	= this;

			InitializeComponent();

			MainPage	= new BakjeClient.MainPage();
		}

		protected override void OnStart()
		{
			// Handle when your app starts
		}

		protected override void OnSleep()
		{
			// Handle when your app sleeps
		}

		protected override void OnResume()
		{
			// Handle when your app resumes
		}

		public static Page GetLoginPage()
		{
			var navPage			= new NavigationPage(new Login());
			instance.MainPage	= navPage;
			navPage.PopToRootAsync();

			return navPage;
		}
	}
}

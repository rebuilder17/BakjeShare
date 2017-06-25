using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Xamarin.Forms;
using Rg.Plugins.Popup.Extensions;

namespace BakjeClient
{
	public partial class App : Application
	{
		public static App instance { get; private set; }
		public Engine.ClientEngine core { get; private set; }
		//public INavigation navigation { get; private set; }
		//NavigationPage	m_navPage;
		Popup.LoadingPopup	m_loadingPopup;

		public App()
		{
			instance	= this;
			core		= new Engine.ClientEngine();
			core.Initialize();

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

		public static void ShowLoading()
		{
			if (instance.m_loadingPopup == null)
			{
				instance.m_loadingPopup	= new Popup.LoadingPopup();
				instance.MainPage.Navigation.PushPopupAsync(instance.m_loadingPopup);
			}
		}

		public static void HideLoading()
		{
			if (instance.m_loadingPopup != null)
			{
				instance.MainPage.Navigation.PopPopupAsync();
				instance.m_loadingPopup = null;
			}
		}
	}
}

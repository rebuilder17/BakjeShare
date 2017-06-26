using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Xamarin.Forms;
using Rg.Plugins.Popup.Extensions;
using System.Threading.Tasks;

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
			
			InitializeComponent();
		}

		protected override void OnStart()
		{
			// Handle when your app starts

			core		= new Engine.ClientEngine();
			core.Initialize();
			MainPage	= new NavigationPage(new ConnectPage());
		}

		protected override void OnSleep()
		{
			// Handle when your app sleeps
		}

		protected override void OnResume()
		{
			// Handle when your app resumes
		}

		public static Page GetConnectPage()
		{
			var navPage			= new NavigationPage(new ConnectPage());
			instance.MainPage	= navPage;
			navPage.PopToRootAsync();

			return navPage;
		}

		public static Page GetLoginPage()
		{
			var navPage			= new NavigationPage(new LoginPage());
			instance.MainPage	= navPage;
			navPage.PopToRootAsync();

			return navPage;
		}

		public static Page GetMainPage()
		{
			//var navPage			= new NavigationPage(new MainPage());
			//instance.MainPage	= navPage;
			//navPage.PopToRootAsync();

			//return navPage;
			instance.MainPage		= new MainPage();
			return instance.MainPage;
		}
		//

		private async static Task ShowLoading()
		{
			if (instance.m_loadingPopup == null)
			{
				instance.m_loadingPopup	= new Popup.LoadingPopup();
				await instance.MainPage.Navigation.PushPopupAsync(instance.m_loadingPopup);
			}
		}

		private async static Task HideLoading()
		{
			if (instance.m_loadingPopup != null)
			{
				await instance.MainPage.Navigation.PopPopupAsync();
				instance.m_loadingPopup = null;
			}
		}

		public static async Task RunLongTask(Action action)
		{
			await ShowLoading();

			try
			{
				await Task.Run(action);
				await HideLoading();
			}
			catch(Exception e)
			{
				await HideLoading();
				throw e;
			}
		}

		public static async Task RunLongTask(Func<Task> action)
		{
			await ShowLoading();

			try
			{
				await Task.Run(action);
				await HideLoading();
			}
			catch(Exception e)
			{
				await HideLoading();
				throw e;
			}
		}
	}
}

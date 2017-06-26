using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace BakjeClient
{
	public partial class ConnectPage : ContentPage
	{
		private class ViewModel
		{
			const string c_keyLastUrl = "lasturl";

			public string urlOrIP { get; set; }

			public ViewModel()
			{
				if (App.Current.Properties.ContainsKey(c_keyLastUrl))
				{
					urlOrIP	= App.Current.Properties[c_keyLastUrl] as string;
				}
			}

			public void SaveCurrentURL()
			{
				App.Current.Properties[c_keyLastUrl] = urlOrIP;
				App.Current.SavePropertiesAsync();
			}
		}

		ViewModel		m_viewModel;

		public ConnectPage()
		{
			InitializeComponent();
			m_viewModel		= new ViewModel();
			BindingContext	= m_viewModel;
		}

		private async void Button_Clicked(object sender, EventArgs e)
		{
			try
			{
				m_viewModel.SaveCurrentURL();

				var core	= App.instance.core;

				var result = Engine.ClientEngine.AuthCheckResult.None;

				try
				{
					await App.RunLongTask(() =>
					{
						core.SetServerURLorIP(m_viewModel.urlOrIP ?? "");
						result = core.auth.CheckAuth();
					});

					switch (result)
					{
						case Engine.ClientEngine.AuthCheckResult.OK:
							await DisplayAlert("로그인", "로그인 확인 완료", "확인");
							App.GetMainPage();
							break;

						case Engine.ClientEngine.AuthCheckResult.LoginNeeded:
							await DisplayAlert("로그인 필요", "로그인해야 합니다.", "확인");
							App.GetLoginPage();
							break;

						case Engine.ClientEngine.AuthCheckResult.CannotConnect:
							await DisplayAlert("오류", "서버에 접속할 수 없습니다.", "확인");
							break;

						default:
							await DisplayAlert("오류", "알 수 없는 오류가 발생했습니다.", "확인");
							break;
					}
				}
				catch(FormatException)
				{
					await DisplayAlert("오류", "url 혹은 ip 주소 형식이 잘못되었습니다.", "확인");
				}
			}
			catch(Exception ex)
			{
				await DisplayAlert("!!", ex.ToString(), "!!");
			}
		}
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace BakjeClient
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class LoginPage : ContentPage
	{
		class ViewModel
		{
			public string id { get; set; }
			public string password { get; set; }
		}

		ViewModel		m_viewModel;

		public LoginPage()
		{
			InitializeComponent();

			m_viewModel		= new ViewModel();
			BindingContext	= m_viewModel;
		}

		private async void BtnLogin_Clicked(object sender, EventArgs e)
		{
			var core	= App.instance.core;
			var result	= false;
			
			await App.RunLongTask(() =>
			{
				result	= App.instance.core.auth.RequestLogin(m_viewModel.id, m_viewModel.password);
			});
			
			if (result)
			{
				await DisplayAlert("로그인", "로그인에 성공하였습니다. "
					+ (core.authLevel == BakjeProtocol.Auth.UserType.Registered ? "(회원)" : "(관리자)"), "확인");
				App.GetMainPage();
			}
			else
			{
				await DisplayAlert("로그인", "로그인에 실패하였습니다. ID 혹은 Password를 확인해주세요.", "확인");
			}
		}

		private async void BtnRegister_Clicked(object sender, EventArgs e)
		{
			await Navigation.PushAsync(new UserRegistrationPage());
		}
	}
}
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
			var result	= BakjeProtocol.Parameters.RespLogin.Status.NoUserInfo;
			
			await App.RunLongTask(() =>
			{
				result	= App.instance.core.auth.RequestLogin(m_viewModel.id, m_viewModel.password);
			});
			
			switch(result)
			{
				case BakjeProtocol.Parameters.RespLogin.Status.OK:
					await DisplayAlert("로그인", "로그인에 성공하였습니다. "
					+ (core.authLevel == BakjeProtocol.Auth.UserType.Registered ? "(회원)" : "(관리자)"), "확인");

					// Auth 시스템에 유저이름을 아는 기능을 안만들어놔서 임시로...
					App.Current.Properties["username"] = m_viewModel.id;
					await App.Current.SavePropertiesAsync();

					App.GetMainPage();
					break;

				case BakjeProtocol.Parameters.RespLogin.Status.NoUserInfo:
					await DisplayAlert("로그인 실패", "로그인에 실패하였습니다. 유저 정보가 없습니다. ID가 올바른지 확인해주세요.", "확인");
					break;

				case BakjeProtocol.Parameters.RespLogin.Status.WrongPassword:
					await DisplayAlert("로그인 실패", "로그인에 실패하였습니다. 패스워드를 확인해주세요.", "확인");
					break;

				case BakjeProtocol.Parameters.RespLogin.Status.BlindedUser:
					await DisplayAlert("로그인 실패", "로그인에 실패하였습니다. 현재 계정이 블라인드처리되어 로그인이 불가능합니다. 운영자에게 문의하세요.", "확인");
					break;
			}
		}

		private async void BtnRegister_Clicked(object sender, EventArgs e)
		{
			await Navigation.PushAsync(new UserRegistrationPage());
		}
	}
}
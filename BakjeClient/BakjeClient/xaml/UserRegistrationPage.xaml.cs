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
	public partial class UserRegistrationPage : ContentPage
	{
		class ViewModel
		{
			public string id { get; set; }
			public string password { get; set; }
			public string passwordConfirm { get; set; }
			public string email { get; set; }
		}

		ViewModel	m_viewModel;

		public UserRegistrationPage()
		{
			InitializeComponent();
			m_viewModel		= new ViewModel();
			BindingContext	= m_viewModel;
		}

		private async void BtnRegister_Clicked(object sender, EventArgs e)
		{
			if (string.IsNullOrWhiteSpace(m_viewModel.id) || string.IsNullOrWhiteSpace(m_viewModel.password) || string.IsNullOrWhiteSpace(m_viewModel.email))
			{
				await DisplayAlert("오류", "필요한 정보를 모두 입력해주세요.", "확인");
			}
			else if (m_viewModel.password != m_viewModel.passwordConfirm)	// password confirm 체크
			{
				await DisplayAlert("오류", "같은 패스워드를 두 번 입력해주세요.", "확인");
			}
			else
			{
				var core	= App.instance.core;
				var result	= Engine.ClientEngine.NewUserResult.None;

				await App.RunLongTask(() =>
				{
					result	= core.user.NewUser(m_viewModel.id, m_viewModel.password, m_viewModel.email);
				});

				switch(result)
				{
					case Engine.ClientEngine.NewUserResult.Success:
						await DisplayAlert("성공", "새 계정이 등록되었습니다. 해당 계정으로 로그인해주세요.", "확인");
						await Navigation.PopAsync();
						break;

					case Engine.ClientEngine.NewUserResult.DuplicatedID:
						await DisplayAlert("오류", "중복된 ID입니다.", "확인");
						break;

					case Engine.ClientEngine.NewUserResult.AlreadyRegisteredEmail:
						await DisplayAlert("오류", "이미 등록된 e-mail입니다.", "확인");
						break;

					default:
						await DisplayAlert("오류", "알 수 없는 오류가 발생하였습니다.", "확인");
						break;
				}
			}
		}

		private async void BtnCancel_Clicked(object sender, EventArgs e)
		{
			await Navigation.PopAsync();
		}
	}
}
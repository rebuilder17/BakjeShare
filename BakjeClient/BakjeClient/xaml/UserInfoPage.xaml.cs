using BakjeProtocol.Parameters;
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
	public partial class UserInfoPage : ContentPage
	{
		class ViewModel
		{
			public string userName { get; set; }
			public string email { get; set; }

			public ViewModel(RespUserInfo info)
			{
				userName	= info.userID;
				email		= info.email;
			}
		}

		ViewModel	m_viewModel;

		public UserInfoPage(RespUserInfo info)
		{
			InitializeComponent();

			m_viewModel	= new ViewModel(info);
			BindingContext	= m_viewModel;
		}

		const string c_actionAllPosting	= "작성한 글 보기...";
		const string c_actionDeleteUser	= "회원 탈퇴";
		const string c_actionBlindUser	= "블라인드 처리";

		private async void BtnAction_Clicked(object sender, EventArgs e)
		{
			var actions = new List<string>();

			actions.Add(c_actionAllPosting);

			if (!App.instance.isAdmin && m_viewModel.userName == (string)App.Current.Properties["username"])
			{
				actions.Add(c_actionDeleteUser);
			}

			if (App.instance.isAdmin)
			{
				actions.Add(c_actionBlindUser);
			}

			var choice = await DisplayActionSheet("무엇을 하시겠습니까?", "취소", null, actions.ToArray());

			switch(choice)
			{
				case c_actionAllPosting:
					{
						RespLookupPosting result = null;
						var searchCond			= new Engine.ClientEngine.PostingLookupCondition { user = m_viewModel.userName };
						await App.RunLongTask(() =>
						{
							result = App.instance.core.post.LookupPosting(searchCond);
						});

						if (result != null)
						{
							await Navigation.PushAsync(new PostingListPage(searchCond, result));
						}
						else
						{
							await DisplayAlert("오류", "포스팅을 조회할 수 없습니다.", "확인");
						}
					}
					break;

				case c_actionDeleteUser:
					{
						var result = await DisplayAlert("확인", "정말로 회원 탈퇴를 하시겠습니까?", "네", "아니오");
						if (result)
						{
							result = await DisplayAlert("확인", "다시 한 번 확인하겠습니다. 정말로 회원 탈퇴를 하시겠습니까?", "네", "아니오");
							if(result)
							{
								await App.RunLongTask(() => App.instance.core.user.DeleteUser());
								await DisplayAlert("회원 탈퇴", "회원 탈퇴를 완료하였습니다. 안녕히가세요...", "확인");
								App.instance.core.auth.ClearAuth();
								App.GetLoginPage();
							}
						}
					}
					break;

				case c_actionBlindUser:
					{
						var result = await DisplayAlert("확인", "정말로 이 유저를 블라인드 처리할까요?", "네", "아니오");
						if(result)
						{
							await App.RunLongTask(() => App.instance.core.user.BlindUser(m_viewModel.userName, true));
							await DisplayAlert("완료", "블라인드 처리를 완료했습니다.", "확인");
							App.GetMainPage();
						}
					}
					break;
			}
		}
	}
}
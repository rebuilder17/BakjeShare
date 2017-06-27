using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using BakjeProtocol.Parameters;

namespace BakjeClient
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class NoticeViewPage : ContentPage
	{
		class ViewModel
		{
			public string title { get; set; }
			public string desc { get; set; }

			public bool isAdmin
			{
				get
				{
					return App.instance.core.authLevel == BakjeProtocol.Auth.UserType.Administrator;
				}
			}

			public ViewModel(RespShowNotice notice)
			{
				title	= notice.title;
				desc	= notice.desc;
			}
		}

		ViewModel	m_viewModel;
		int			m_noticeId;

		public NoticeViewPage(int noticeId, RespShowNotice notice)
		{
			InitializeComponent();
			m_noticeId	= noticeId;
			m_viewModel	= new ViewModel(notice);
			BindingContext	= m_viewModel;
		}

		private async void BtnDelete_Clicked(object sender, EventArgs e)
		{
			await App.RunLongTask(() =>
			{
				App.instance.core.notice.DeleteNotice(m_noticeId);
			});
			await DisplayAlert("완료", "삭제하였습니다", "확인");
			App.GetMainPage();
		}
	}
}
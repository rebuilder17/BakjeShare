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
	public partial class NewReportUserPage : ContentPage
	{
		class ViewModel
		{
			public string user { get; set; }
			public string title { get; set; }
			public string desc { get; set; }

			public ViewModel(string username)
			{
				user = username;
			}
		}
		ViewModel m_viewModel;

		public NewReportUserPage(string user)
		{
			InitializeComponent();

			m_viewModel	= new ViewModel(user);
			BindingContext	= m_viewModel;
		}

		private async void BtnSend_Clicked(object sender, EventArgs e)
		{
			await App.RunLongTask(() =>
			{
				App.instance.core.report.FileReport_User(m_viewModel.title, m_viewModel.desc, m_viewModel.user,
					BakjeProtocol.Parameters.ReqFileReport.UserReportReason.Etc);
			});

			await DisplayAlert("완료", "유저 신고를 보냈습니다. 감사합니다!", "확인");
			await Navigation.PopAsync();
		}
	}
}
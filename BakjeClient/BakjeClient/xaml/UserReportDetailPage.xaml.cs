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
	public partial class UserReportDetailPage : ContentPage
	{
		class ViewModel
		{
			public string title { get; set; }
			public string desc { get; set; }
			public string reporter { get; set; }
			public string reported { get; set; }

			public ViewModel(RespShowReport report)
			{
				title		= report.shortdesc;
				desc		= report.longdesc;
				reporter	= report.reporterID;
				reported	= report.repUserID;
			}
		}

		ViewModel m_viewModel;
		int m_reportID;

		public UserReportDetailPage(int reportID, RespShowReport report)
		{
			InitializeComponent();
			m_reportID	= reportID;
			m_viewModel	= new ViewModel(report);
			BindingContext = m_viewModel;
		}

		private async void BtnDelete_Clicked(object sender, EventArgs e)
		{
			await App.RunLongTask(() =>
			{
				App.instance.core.report.CloseReport(m_reportID);
			});

			App.GetMainPage();
		}

		private async void BtnShow_Clicked(object sender, EventArgs e)
		{
			RespUserInfo result = null;
			await App.RunLongTask(() =>
			{
				result = App.instance.core.user.ShowUserInfo(m_viewModel.reported);
			});

			if (result != null)
			{
				await Navigation.PushAsync(new UserInfoPage(result));
			}
		}
	}
}
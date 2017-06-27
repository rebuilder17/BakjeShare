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
	public partial class PostingReportDetailPage : ContentPage
	{
		class ViewModel
		{
			public string title { get; set; }
			public string desc { get; set; }
			public string reporter { get; set; }
			public string reportedTitle { get; set; }
			public int reportedID { get; set; }

			public ViewModel(RespShowReport report)
			{
				title		= report.shortdesc;
				desc		= report.longdesc;
				reporter	= report.reporterID;
				reportedTitle	= report.repPostingTitle;
				reportedID	= report.repPostingID;
			}
		}

		ViewModel m_viewModel;
		int m_reportID;

		public PostingReportDetailPage(int reportID, RespShowReport report)
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
			Engine.ClientEngine.PostingDetail result = null;
			await App.RunLongTask(() =>
			{
				result = App.instance.core.post.ShowPosting(m_viewModel.reportedID);
			});

			if (result != null)
			{
				await Navigation.PushAsync(new PostingDetailPage(m_viewModel.reportedID, result));
			}
		}
	}
}
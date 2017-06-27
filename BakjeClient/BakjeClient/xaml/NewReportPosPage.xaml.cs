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
	public partial class NewReportPosPage : ContentPage
	{
		class ViewModel
		{
			public string user { get; set; }
			public string title { get; set; }
			public string desc { get; set; }

			public string postingtitle { get; set; }
			public int postingid { get; set; }

			public ViewModel(string postingtitle, int postingid)
			{
				this.postingtitle = postingtitle;
				this.postingid	= postingid;
			}
		}
		ViewModel m_viewModel;

		public NewReportPosPage(int postid, string postTitle)
		{
			InitializeComponent();

			m_viewModel	= new ViewModel(postTitle, postid);
			BindingContext	= m_viewModel;
		}

		private async void BtnSend_Clicked(object sender, EventArgs e)
		{
			await App.RunLongTask(() =>
			{
				App.instance.core.report.FileReport_Posting(m_viewModel.title, m_viewModel.desc, m_viewModel.postingid, 
					BakjeProtocol.Parameters.ReqFileReport.PostReportReason.Etc);
			});

			await DisplayAlert("완료", "포스팅 신고를 보냈습니다. 감사합니다!", "확인");
			await Navigation.PopAsync();
		}
		
	}
}
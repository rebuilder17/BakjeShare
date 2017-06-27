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
	public partial class NewBugReportPage : ContentPage
	{
		class ViewModel
		{
			public string title { get; set; }
			public string desc { get; set; }
		}
		ViewModel m_viewModel;

		public NewBugReportPage()
		{
			InitializeComponent();
			m_viewModel		= new ViewModel();
			BindingContext	= m_viewModel;
		}

		private async void BtnSend_Clicked(object sender, EventArgs e)
		{
			await App.RunLongTask(() =>
			{
				App.instance.core.report.FileReport_Bug(m_viewModel.title, m_viewModel.desc);
			});

			await DisplayAlert("완료", "버그리포트를 보냈습니다. 감사합니다!", "확인");
			App.GetMainPage();
		}
	}
}
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
	public partial class NewNoticePage : ContentPage
	{
		class ViewModel
		{
			public string title { get; set; }
			public string desc { get; set; }
		}

		ViewModel m_viewModel;

		public NewNoticePage()
		{
			InitializeComponent();
			m_viewModel		= new ViewModel();
			BindingContext	= m_viewModel;
		}

		private async void BtnSend_Clicked(object sender, EventArgs e)
		{
			await App.RunLongTask(() =>
			{
				App.instance.core.notice.PostNotice(m_viewModel.title, m_viewModel.desc);
			});

			await DisplayAlert("완료", "공지를 올렸습니다.", "확인");
			App.GetMainPage();
		}
	}
}
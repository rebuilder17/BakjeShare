using BakjeProtocol.Parameters;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace BakjeClient
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class LookupReportPage : ContentPage
	{
		class ViewModel
		{
			public class Item
			{
				public string title { get; set; }
				public string detail { get; set; }
				
				public int reportid { get; set; }
				
				public Item(RespLookupReport.Entry entry)
				{
					reportid	= entry.reportID;

					title		= entry.shortdesc;
					
					switch(entry.type)
					{
						case ReqFileReport.Type.Bug:
							detail	= "버그리포트";
							break;

						case ReqFileReport.Type.Posting:
							detail	= "포스팅 신고";
							break;

						case ReqFileReport.Type.User:
							detail	= "유저 신고";
							break;
					}
				}
			}

			public string title { get; set; }
			public string pageStatus { get; set; }

			public bool canGoPrev { get; set; }
			public bool canGoNext { get; set; }

			public ObservableCollection<Item> items { get; }
			

			public ViewModel(RespLookupReport lookup)
			{
				pageStatus		= string.Format("{0}/{1}", lookup.currentPage + 1, lookup.totalPage);

				canGoPrev		= lookup.currentPage > 0;
				canGoNext		= (lookup.currentPage + 1) < lookup.totalPage;

				items			= new ObservableCollection<Item>();
				foreach (var entry in lookup.entries)
				{
					items.Add(new Item(entry));
				}
			}
		}

		ViewModel	m_viewModel;


		public LookupReportPage(RespLookupReport lookup)
		{
			InitializeComponent();

			m_viewModel	= new ViewModel(lookup);
			BindingContext	= m_viewModel;
		}

		private async void Handle_ItemTapped(object sender, ItemTappedEventArgs e)
		{
			if (e.Item != null)
			{
				var data = postList.SelectedItem as ViewModel.Item;
				postList.SelectedItem = null;

				RespShowReport result = null;
				await App.RunLongTask(() =>
				{
					result = App.instance.core.report.ShowReport(data.reportid);
				});

				if (result == null)
				{
					
				}
				else
				{
					switch (result.type)
					{
						case ReqFileReport.Type.Bug:
							await Navigation.PushAsync(new BugReportDetailPage(data.reportid, result));
							break;

						case ReqFileReport.Type.Posting:
							await Navigation.PushAsync(new PostingReportDetailPage(data.reportid, result));
							break;

						case ReqFileReport.Type.User:
							await Navigation.PushAsync(new UserReportDetailPage(data.reportid, result));
							break;
					}
				}
			}
		}
	}
}
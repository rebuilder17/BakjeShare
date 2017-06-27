using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using BakjeProtocol.Parameters;
using System.Collections.ObjectModel;

namespace BakjeClient
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class NoticeListPage : ContentPage
	{
		class ViewModel
		{
			public class Item
			{
				public string title { get; set; }
				public string detail { get; set; }

				public int noticeId { get; set; }

				public Item(RespLookupNotice.Entry entry)
				{
					noticeId	= entry.noticeID;

					title	= entry.title;
					detail	= string.Format("공지 일시 : {0}", entry.datetime.ToString());
				}
			}
			
			public string pageStatus { get; set; }

			public bool canGoPrev { get; set; }
			public bool canGoNext { get; set; }

			public ObservableCollection<Item> items { get; }
			

			public ViewModel(RespLookupNotice lookup)
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

		public NoticeListPage(RespLookupNotice notice)
		{
			InitializeComponent();
			try
			{
				m_viewModel		= new ViewModel(notice);
				BindingContext	= m_viewModel;
			}
			catch(Exception e)
			{
				DisplayAlert("!!", e.ToString(), "...");
			}
		}

		private async void Handle_ItemTapped(object sender, ItemTappedEventArgs e)
		{
			if (e.Item != null)
			{
				var data = postList.SelectedItem as ViewModel.Item;
				RespShowNotice posting = null;

				postList.SelectedItem = null;

				await App.RunLongTask(() =>
				{
					posting	= App.instance.core.notice.ShowNotice(data.noticeId);
				});

				if (posting == null)
				{
					await DisplayAlert("오류", "포스팅을 읽어올 수 없습니다.", "확인");
				}
				else
				{
					await Navigation.PushAsync(new NoticeViewPage(data.noticeId, posting));
				}
			}
		}
	}
}
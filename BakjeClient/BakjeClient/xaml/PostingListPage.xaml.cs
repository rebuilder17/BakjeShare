using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using BakjeProtocol.Parameters;

namespace BakjeClient
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class PostingListPage : ContentPage
	{
		class ViewModel
		{
			public class Item
			{
				public string title { get; set; }
				public string detail { get; set; }

				public Item(RespLookupPosting.Entry entry)
				{
					title	= entry.title;
					detail	= string.Format("작성자 : {0}    작성일시 : {1}", entry.author, entry.postingTime.ToString());
				}
			}

			public string searchCondition { get; set; }
			public string pageStatus { get; set; }

			public bool canGoPrev { get; set; }
			public bool canGoNext { get; set; }

			public ObservableCollection<Item> items { get; }

			static string MakeConditionText(Engine.ClientEngine.PostingLookupCondition searchCond)
			{
				var strBuilder		= new System.Text.StringBuilder("검색 조건 -");
				var anySearchCond	= false;

				var titleAvail		= !string.IsNullOrWhiteSpace(searchCond.title);
				var descAvail		= !string.IsNullOrWhiteSpace(searchCond.desc);

				if (titleAvail && descAvail && searchCond.title == searchCond.desc)
				{
					strBuilder.Append(" 글+제목:").Append(searchCond.title);
					anySearchCond	= true;
				}
				else
				{
					if (titleAvail)
					{
						strBuilder.Append(" 제목:").Append(searchCond.title);
						anySearchCond	= true;
					}
					if (descAvail)
					{
						strBuilder.Append(" 글:").Append(searchCond.desc);
						anySearchCond	= true;
					}
				}

				if (!string.IsNullOrWhiteSpace(searchCond.user))
				{
					strBuilder.Append(" 글쓴이:").Append(searchCond.user);
					anySearchCond	= true;
				}

				if (!string.IsNullOrWhiteSpace(searchCond.tag))
				{
					strBuilder.Append(" 태그:").Append(searchCond.tag);
					anySearchCond	= true;
				}

				return anySearchCond ? strBuilder.ToString() : "최근 글";
			}

			public ViewModel(Engine.ClientEngine.PostingLookupCondition searchCond, RespLookupPosting lookup)
			{
				searchCondition	= MakeConditionText(searchCond ?? Engine.ClientEngine.PostingLookupCondition.empty);
				pageStatus		= string.Format("{0}/{1}", lookup.currentPage + 1, lookup.totalPage);

				canGoPrev		= lookup.currentPage > 0;
				canGoNext		= (lookup.currentPage + 1) < lookup.totalPage;

				items			= new ObservableCollection<Item>();
				foreach(var entry in lookup.entries)
				{
					items.Add(new Item(entry));
				}
			}
		}

		ViewModel	m_viewModel;
		Engine.ClientEngine.PostingLookupCondition	m_searchCondition;

		public PostingListPage(Engine.ClientEngine.PostingLookupCondition searchCond, RespLookupPosting lookup)
		{
			InitializeComponent();
			m_searchCondition	= searchCond;
			m_viewModel			= new ViewModel(searchCond, lookup);
			BindingContext		= m_viewModel;
		}

		private void Handle_ItemTapped(object sender, ItemTappedEventArgs e)
		{
			if (e.Item != null)
			{
				postList.SelectedItem = null;
			}
		}
	}
}
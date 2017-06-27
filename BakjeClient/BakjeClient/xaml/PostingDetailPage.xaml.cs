using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using BakjeProtocol.Parameters;

namespace BakjeClient
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class PostingDetailPage : ContentPage
	{
		class ViewModel
		{
			public class ImageItem
			{
				public byte[] image { get; set; }
			}

			public class TagItem
			{
				public string tag { get; set; }
			}

			public ObservableCollection<ImageItem> images { get; set; }
			
			public string title { get; set; }
			public string author { get; set; }
			public string datetime { get; set; }
			
			public string detail { get; set; }

			public ObservableCollection<TagItem> myTagItems { get; set; }
			public ObservableCollection<TagItem> otherTagItems { get; set; }

			public ViewModel(Engine.ClientEngine.PostingDetail postdata)
			{
				images			= new ObservableCollection<ImageItem>();
				foreach (var img in postdata.imageArray)
				{
					images.Add(new ImageItem { image = img });
				}

				var posting		= postdata.postingInfo;
				title			= posting.title;
				author			= posting.author;
				datetime		= posting.datetime.ToString();
				detail			= posting.desc;

				myTagItems		= new ObservableCollection<TagItem>();
				foreach (var tag in posting.mytags)
				{
					myTagItems.Add(new TagItem() { tag = tag });
				}
				otherTagItems	= new ObservableCollection<TagItem>();
				foreach (var tag in posting.othertags)
				{
					otherTagItems.Add(new TagItem() { tag = tag });
				}
			}
		}

		int			m_postingID;
		ViewModel	m_viewModel;

		public PostingDetailPage(int postingId, Engine.ClientEngine.PostingDetail postingData)
		{
			InitializeComponent();

			// 우선은 태그 입력 필드를 안보이게
			myTagEntryView.TagEntry.IsVisible		= false;
			otherTagEntryView.TagEntry.IsVisible	= false;

			m_postingID		= postingId;
			m_viewModel		= new ViewModel(postingData);
			BindingContext	= m_viewModel;
		}
		
		private void Handle_ItemTapped(object sender, ItemTappedEventArgs e)
		{
			if (e.Item != null)
			{
				contentList.SelectedItem = null;
			}
		}
	}
}
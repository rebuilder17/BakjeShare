using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Rg.Plugins.Popup.Extensions;
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

			public string origUrl { get; set; }

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

				origUrl			= posting.sourceURL;

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

			if (!string.IsNullOrEmpty(postingData.postingInfo.sourceURL))	// 링크 추가
			{
				Uri uri = null;

				try
				{
					uri = new Uri(postingData.postingInfo.sourceURL);
				}
				catch
				{ }

				if (uri != null)
				{
					var gestureRec = new TapGestureRecognizer();
					gestureRec.Tapped += (s, e) =>
					{
						Device.OpenUri(uri);
					};

					linkToSource.GestureRecognizers.Add(gestureRec);
				}
			}
		}
		
		private void Handle_ItemTapped(object sender, ItemTappedEventArgs e)
		{
			if (e.Item != null)
			{
				contentList.SelectedItem = null;
			}
		}

		const string c_actionDelete			= "이 글을 삭제합니다.";
		const string c_actionUserInfo		= "글쓴이 정보를 봅니다.";
		const string c_actionReportAuthor	= "글쓴이를 신고합니다.";
		const string c_actionReportPost		= "포스팅을 신고합니다.";
		const string c_actionBlindAuthor	= "글쓴이 블라인드 처리";
		const string c_actionBlindPost		= "포스팅을 블라인드 처리";

		private async void BtnAction_Clicked(object sender, EventArgs e)
		{
			var actions = new List<string>();

			if (m_viewModel.author == (string)App.Current.Properties["username"])
			{
				actions.Add(c_actionDelete);
			}
			else
			{
				actions.Add(c_actionUserInfo);
			}
			
			if (App.instance.isAdmin)
			{
				actions.Add(c_actionBlindPost);
				actions.Add(c_actionBlindAuthor);
			}
			else
			{
				actions.Add(c_actionReportPost);
				actions.Add(c_actionReportAuthor);
			}

			var choice = await DisplayActionSheet("무엇을 하시겠습니까?", "취소", null, actions.ToArray());

			switch(choice)
			{
				case c_actionUserInfo:
					{
						RespUserInfo result = null;
						await App.RunLongTask(() =>
						{
							result = App.instance.core.user.ShowUserInfo(m_viewModel.author);
						});

						if (result != null)
						{
							await Navigation.PushAsync(new UserInfoPage(result));
						}
					}
					break;
				case c_actionDelete:
					{
						var result	= await DisplayAlert("삭제 확인", "정말로 글을 삭제하시겠습니까?", "네", "아니오");
						if(result)
						{
							await App.RunLongTask(() =>
							{
								App.instance.core.post.DeletePost(m_postingID);
							});
							await DisplayAlert("삭제 완료", "글을 삭제하였습니다.", "확인");

							App.GetMainPage();
						}
					}
					break;

				case c_actionReportPost:
					{
						await Navigation.PushAsync(new NewReportPosPage(m_postingID, m_viewModel.title));
					}
					break;

				case c_actionReportAuthor:
					{
						await Navigation.PushAsync(new NewReportUserPage(m_viewModel.author));
					}
					break;

				case c_actionBlindPost:
					{
						var result = await DisplayAlert("블라인드 처리", "해당 포스팅을 정말로 블라인드 처리 하시겠습니까? 이 포스팅은 본인과 운영자 외엔 아무도 열람할 수 없게 됩니다.", "네", "아니오");
						if (result)
						{
							await App.RunLongTask(() =>
							{
								App.instance.core.post.BlindPost(m_postingID, true);
							});
							await DisplayAlert("블라인드 처리", "해당 포스팅이 블라인드 처리 되었습니다.", "확인");

							App.GetMainPage();
						}
					}
					break;

				case c_actionBlindAuthor:
					{
						var result = await DisplayAlert("블라인드 처리", "해당 유저를 정말로 블라인드 처리 하시겠습니까? 유저가 작성한 모든 포스팅이 블라인드 처리된 것처럼 취급되며 해당 유저는 더이상 로그인할 수 없게 됩니다.", "네", "아니오");
						if (result)
						{
							await App.RunLongTask(() =>
							{
								App.instance.core.user.BlindUser(m_viewModel.author, true);
							});
							await DisplayAlert("블라인드 처리", "해당 유저가 블라인드 처리 되었습니다.", "확인");

							App.GetMainPage();
						}
					}
					break;
			}
		}

		private async void BtnAddTag_Clicked(object sender, EventArgs e)
		{
			var inputPage = new Popup.EntryPopup("태그를 입력해주세요");
			inputPage.Disappearing += async (s, evp) =>
			{
				var tagstr = inputPage.InputValue;
				if (tagstr != null)
				{
					await App.RunLongTask(() =>
					{
						App.instance.core.post.AddTag(m_postingID, tagstr);
					});

					m_viewModel.myTagItems.Add(new ViewModel.TagItem { tag = tagstr });
				}
			};

			await Navigation.PushPopupAsync(inputPage);
		}

		const string c_tagActionSearch = "이 태그로 검색";
		const string c_tagActionDelete = "이 태그 삭제";

		private async void myTagEntryView_TagTapped(object sender, ItemTappedEventArgs e)
		{
			if (e.Item != null)
			{
				var tagitem = e.Item as ViewModel.TagItem;

				var choice = await DisplayActionSheet(string.Format("태그 '{0}' 에 대해서...", tagitem.tag), "취소", null, new [] {c_tagActionSearch, c_tagActionDelete});

				switch(choice)
				{
					case c_tagActionSearch:
						{
							var cond = new Engine.ClientEngine.PostingLookupCondition { tag = tagitem.tag };

							RespLookupPosting result = null;
							await App.RunLongTask(() =>
							{
								result = App.instance.core.post.LookupPosting(cond);
							});

							if(result != null)
							{
								await Navigation.PushAsync(new PostingListPage(cond, result));
							}
						}
						break;

					case c_tagActionDelete:
						{
							await App.RunLongTask(() =>
							{
								App.instance.core.post.DeleteTag(m_postingID, tagitem.tag);
							});

							m_viewModel.myTagItems.Remove(tagitem);
						}
						break;
				}
			}
		}
	}
}
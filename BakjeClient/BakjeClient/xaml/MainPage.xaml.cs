using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using BakjeProtocol.Parameters;

namespace BakjeClient
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class MainPage : MasterDetailPage
	{
		Dictionary<string, Action>		m_idToActionDict	= new Dictionary<string, Action>();
		Dictionary<string, Func<Task>>	m_idToAsyncDict		= new Dictionary<string, Func<Task>>();

		public MainPage()
		{
			InitializeComponent();
			InitActions();
			MasterPage.ListView.ItemSelected += ListView_ItemSelected;
		}

		private void AddAction(string id, Action action)
		{
			m_idToActionDict[id] = action;
		}

		private void AddAsyncAction(string id, Func<Task> action)
		{
			m_idToAsyncDict[id] = action;
		}

		private async void ListView_ItemSelected(object sender, SelectedItemChangedEventArgs e)
		{
			var item = e.SelectedItem as MainPageMenuItem;
			if (item == null)
				return;
			
			if (item.TargetType != null)				// 타겟 페이지를 지정한 경우, 페이지 열기
			{
				var page = (Page)Activator.CreateInstance(item.TargetType);
				page.Title = item.Title;

				Detail = new NavigationPage(page);
				IsPresented = false;
			}
			else
			{											// 지정하지 않은 경우엔 등록한 액션 실행
				IsPresented = false;

				if (m_idToAsyncDict.ContainsKey(item.Id))
				{
					await m_idToAsyncDict[item.Id]();
				}
				else if (m_idToActionDict.ContainsKey(item.Id))
				{
					m_idToActionDict[item.Id]();
				}
			}

			MasterPage.ListView.SelectedItem = null;
		}

		private void InitActions()
		{
			AddAsyncAction("recentPostings", async () =>
			{
				RespLookupPosting result = null;
				await App.RunLongTask(() =>
				{
					result = App.instance.core.post.LookupPosting(null);
				});

				if (result == null)
				{
					await DisplayAlert("오류", "포스팅을 읽어올 수 없습니다.", "확인");
				}
				else
				{
					Detail = new NavigationPage(new PostingListPage(null, result));
				}
			});
			
			AddAsyncAction("notice", async () =>
			{
				RespLookupNotice result = null;
				await App.RunLongTask(() =>
				{
					result = App.instance.core.notice.LookupNotice();
				});

				if (result == null)
				{
					await DisplayAlert("오류", "포스팅을 읽어올 수 없습니다.", "확인");
				}
				else
				{
					Detail = new NavigationPage(new NoticeListPage(result));
				}
			});

			AddAsyncAction("logout", async () =>
			{
				await Task.Run(() =>
				{
					App.instance.core.auth.ClearAuth();
				});
				await DisplayAlert("로그아웃", "로그아웃하였습니다.", "확인");
				App.GetLoginPage();
			});
		}
	}
}
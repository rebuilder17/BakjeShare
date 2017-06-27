using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace BakjeClient
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class MainPageMaster : ContentPage
	{
		public ListView ListView;

		public MainPageMaster()
		{
			InitializeComponent();

			BindingContext = new MainPageMasterViewModel();
			ListView = MenuItemsListView;
		}

		class MainPageMasterViewModel : INotifyPropertyChanged
		{
			public ObservableCollection<MainPageMenuItem> MenuItems { get; set; }
			public string greetings { get; set; }

			public MainPageMasterViewModel()
			{
				var isAdmin	= App.instance.core.authLevel == BakjeProtocol.Auth.UserType.Administrator;

				if (isAdmin)
				{
					greetings = "관리자님 수고가 많으세요!";
				}
				else
				{
					greetings = string.Format("{0}님 안녕하세요!", (string)App.Current.Properties["username"]);
				}

				MenuItems = new ObservableCollection<MainPageMenuItem>();

				MenuItems.Add(new MainPageMenuItem { Id = "recentPostings", Title = "최근 박제 보기" });
				if (!isAdmin)
				{
					MenuItems.Add(new MainPageMenuItem { Title = "새 박제 올리기", TargetType = typeof(NewPostingPage) });
				}
				MenuItems.Add(new MainPageMenuItem { Id = "notice", Title = "공지사항" });
				
				if (isAdmin)
				{
					MenuItems.Add(new MainPageMenuItem { Title = "새 공지 작성", TargetType = typeof(NewNoticePage) });
					MenuItems.Add(new MainPageMenuItem { Id = "recentReports", Title = "신고 목록" });
				}
				else
				{
					MenuItems.Add(new MainPageMenuItem { Id = "myinfo", Title = "내 정보" });
					MenuItems.Add(new MainPageMenuItem { Title = "버그리포트", TargetType = typeof(NewBugReportPage) });
				}

				MenuItems.Add(new MainPageMenuItem { Id = "logout", Title = "로그아웃" });
			}

			#region INotifyPropertyChanged Implementation
			public event PropertyChangedEventHandler PropertyChanged;
			void OnPropertyChanged([CallerMemberName] string propertyName = "")
			{
				if (PropertyChanged == null)
					return;

				PropertyChanged.Invoke(this, new PropertyChangedEventArgs(propertyName));
			}
			#endregion
		}
	}
}
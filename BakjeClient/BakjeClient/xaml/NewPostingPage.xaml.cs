using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using Plugin.Media;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace BakjeClient
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class NewPostingPage : ContentPage
	{
		class ViewModel : INotifyPropertyChanged
		{
			public class ImageItem : INotifyPropertyChanged
			{
				byte[] m_realData;
				public byte[] imageOriginal
				{
					get
					{
						return m_realData;
					}
					set
					{
						m_realData = value;
						OnPropertyChanged("imageOriginal");
					}
				}
				//public ImageSource thumbnail { get; set; }

				public event PropertyChangedEventHandler PropertyChanged;

				void OnPropertyChanged([CallerMemberName] string propertyName = null)
				{
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
				}
			}

			public string title { get; set; }
			public string desc { get; set; }
			public string originalURL { get; set; }
			public bool isPrivate { get; set; }

			public ObservableCollection<ImageItem> images { get; set; }

			public event PropertyChangedEventHandler PropertyChanged;

			public void AddImage(byte[] data)
			{
				images.Add(new ImageItem { imageOriginal = data });
				OnPropertyChanged("images");
			}

			public ViewModel()
			{
				images	= new ObservableCollection<ImageItem>();
			}

			void OnPropertyChanged([CallerMemberName] string propertyName = null)
			{
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
			}
		}

		ViewModel	m_viewModel;

		public NewPostingPage()
		{
			InitializeComponent();
			m_viewModel		= new ViewModel();
			BindingContext	= m_viewModel;
		}

		private async void BtnAddImage_Clicked(object sender, EventArgs e)
		{
			var photo	= await CrossMedia.Current.PickPhotoAsync();
			if (photo != null)
			{
				//await DisplayAlert("test", photo.Path, "ok");

				byte[] data;
				using (var stream = photo.GetStream())
				{
					var length	= stream.Length;
					data		= new byte[length];
					stream.Read(data, 0, (int)length);

					//await DisplayAlert("test", "length : " + length.ToString(), "ok");
				}

				m_viewModel.AddImage(data);
				imageList.Render();	// 강제 렌더링
			}
		}

		private async void BtnSend_Clicked(object sender, EventArgs e)
		{
			if (string.IsNullOrWhiteSpace(m_viewModel.title) || string.IsNullOrWhiteSpace(m_viewModel.desc))
			{
				await DisplayAlert("오류", "제목과 내용은 채워주셔야 합니다.", "확인");
			}
			else if(m_viewModel.images.Count == 0)
			{
				await DisplayAlert("오류", "스크린샷은 최소 한 개 이상 등록하여야 합니다.", "확인");
			}
			else
			{
				await App.RunLongTask(() =>
				{
					var byteList	= new List<byte[]>();
					foreach (var item in m_viewModel.images)
						byteList.Add(item.imageOriginal);
					App.instance.core.post.NewPost(m_viewModel.title, m_viewModel.desc, m_viewModel.originalURL, m_viewModel.isPrivate, byteList);
				});

				await DisplayAlert("완료", "포스팅을 올렸습니다.", "확인");

				App.GetMainPage();
			}
		}
	}
}
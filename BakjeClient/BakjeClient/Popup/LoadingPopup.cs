using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rg.Plugins.Popup.Pages;
using Rg.Plugins.Popup.Extensions;
using Xamarin.Forms;

namespace BakjeClient.Popup
{
	public class LoadingPopup : PopupPage
	{
		public LoadingPopup()
		{
			Content	= new StackLayout()
			{
				BackgroundColor	= Color.Transparent,
				Children =
				{
					new Label()
					{
						Text		= "잠시만 기다려주세요...",
						FontSize	= 18,
						TextColor	= Color.White,
						HorizontalTextAlignment = TextAlignment.Center,
					},
					new ActivityIndicator()
					{
						IsRunning	= true,
					},
				},
				VerticalOptions = LayoutOptions.Center,
			};

			BackgroundColor	= new Color(0, 0, 0, 0.4);
		}

		protected override bool OnBackButtonPressed()
		{
			return true;
		}

		protected override bool OnBackgroundClicked()
		{
			//return base.OnBackgroundClicked();
			return false;
		}
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rg.Plugins.Popup.Pages;
using Rg.Plugins.Popup.Extensions;
using Xamarin.Forms;

namespace BakjeClient.Popup
{
	public class EntryPopup : PopupPage
	{
		public string InputValue { get; private set; }

		Entry	m_entry;

		public EntryPopup(string title)
		{

			var button = new Button()
			{
				Text	= "추가하기",
				HorizontalOptions = LayoutOptions.Center,
			};
			button.Clicked += Button_Clicked;

			m_entry = new Entry()
			{
				FontSize	= 24,
				WidthRequest = 300,
				HorizontalOptions = LayoutOptions.Center,
			};

			Content	= new StackLayout()
			{
				BackgroundColor	= Color.White,
				Padding	= 20,
				Children =
				{
					new Label()
					{
						Text		= title,
						FontSize	= 24,
						TextColor	= Color.DarkGray,
						HorizontalTextAlignment = TextAlignment.Center,
					},
					m_entry,
					button,
				},
				VerticalOptions = LayoutOptions.Center,
			};

			BackgroundColor	= new Color(0, 0, 0, 0.4);
		}

		private async void Button_Clicked(object sender, EventArgs e)
		{
			if (string.IsNullOrWhiteSpace(m_entry.Text))
			{
				
			}
			else
			{
				InputValue	= m_entry.Text;
				await Navigation.PopPopupAsync();
			}
		}
	}
}
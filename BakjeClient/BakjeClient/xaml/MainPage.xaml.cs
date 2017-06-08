using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace BakjeClient
{
	public partial class MainPage : ContentPage
	{
		public MainPage()
		{
			InitializeComponent();

			Device.StartTimer(TimeSpan.FromMilliseconds(1000), () =>
			{
				App.GetLoginPage();
				return false;
			});
		}
	}
}

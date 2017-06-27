﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace BakjeClient.ValueConverters
{
	public class ByteArrayToImageFieldConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			ImageSource retSource = null;
			if (value != null)
			{
				byte[] imageAsBytes = (byte[])value;
				retSource = ImageSource.FromStream(() => new MemoryStream(imageAsBytes));
			}
			return retSource;
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}

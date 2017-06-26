using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BakjeClient
{

	public class MainPageMenuItem
	{
		public MainPageMenuItem()
		{
			//TargetType = typeof(MainPageDetail);
		}
		public string Id { get; set; }
		public string Title { get; set; }

		public Type TargetType { get; set; }
	}
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BakjeShareServer
{
	class Program
	{
		static void Main(string[] args)
		{
			// Server Test

			var server	= new Http.Server();
			server.Start();

			Console.Out.WriteLine("Server started. any key to close the server...");
			Console.ReadKey();

			server.Stop();
		}
	}
}

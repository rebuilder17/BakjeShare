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

			server.RegisterContextProcessor("/", (context) =>
			{
				var message				= Encoding.UTF8.GetBytes("THIS IS ROOT");

				var resp				= context.Response;
				resp.StatusCode			= 200;
				//resp.ContentType		= "plain/text";
				resp.ContentEncoding	= Encoding.UTF8;
				resp.ContentLength64	= message.Length;
				resp.OutputStream.Write(message, 0, message.Length);
				resp.Close();
			});

			server.RegisterContextProcessor("/test/", (context) =>
			{
				var message				= Encoding.UTF8.GetBytes("this is test suburl");

				var resp				= context.Response;
				resp.StatusCode			= 200;
				//resp.ContentType		= "plain/text";
				resp.ContentEncoding	= Encoding.UTF8;
				resp.ContentLength64	= message.Length;
				resp.OutputStream.Write(message, 0, message.Length);
				resp.Close();
			});


			server.Start();

			Console.Out.WriteLine("Server started. any key to close the server...");
			Console.ReadKey();

			server.Stop();
		}
	}
}

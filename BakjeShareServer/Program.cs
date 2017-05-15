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
			//BasicServerTest();
			BasicPacketTest();
		}

		/// <summary>
		/// Basic Server Test
		/// </summary>
		static void BasicServerTest()
		{
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

		static void BasicPacketTest()
		{
			var packToSend	= new BakjeProtocol.Packet();
			packToSend.SetPlainText("this is plain text");
			packToSend.AddBinaryData(BitConverter.GetBytes(12345));

			var packetData	= packToSend.Pack();
			//

			var packRecv	= BakjeProtocol.Packet.Unpack(packetData);

			Console.Out.WriteLine("packet plain text : {0}", packRecv.GetPlainText());
			Console.Out.WriteLine("packet data : {0}", BitConverter.ToInt32(packRecv.GetBinaryData(0), 0));

			Console.ReadKey();
		}
	}
}

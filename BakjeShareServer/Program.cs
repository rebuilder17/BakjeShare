using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using BakjeProtocol;
using System.Threading;

namespace BakjeShareServer
{
	class Program
	{
		static void Main(string[] args)
		{
			//BasicServerTest();
			//BasicPacketTest();
			BakjeServerTest();
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
			packToSend.header.messageType	= "test";
			packToSend.header.authKey		= null;
			packToSend.SetPlainText("this is plain text");
			packToSend.AddBinaryData(BitConverter.GetBytes(12345));

			var packetData	= packToSend.Pack();
			//

			var packRecv	= BakjeProtocol.Packet.Unpack(packetData);

			Console.Out.WriteLine("header.messageType : {0}", packRecv.header.messageType);
			Console.Out.WriteLine("header.authKey : {0}", packRecv.header.authKey);
			Console.Out.WriteLine("packet plain text : {0}", packRecv.GetPlainText());
			Console.Out.WriteLine("packet data : {0}", BitConverter.ToInt32(packRecv.GetBinaryData(0), 0));

			Console.ReadKey();
		}

		class TestSimpleMessageParam
		{
		}

		class TestMessageParam1
		{
			public string message;
			public int data;
		}

		class TestMessageParam2
		{
			public string message;
			public int[] array;
		}
		
		class TestClient
		{
			class TestClientBridge : BaseProcedurePool.IBridge
			{
				public BaseProcedurePool.IPoolController	m_poolCtrl;

				public void SetPoolController(BaseProcedurePool.IPoolController ctrler)
				{
					m_poolCtrl	= ctrler;
				}
			}

			ClientProcedurePool m_procPool;
			TestClientBridge	m_bridge;

			public TestClient()
			{
				m_procPool	= new ClientProcedurePool();
				m_procPool.AddPairParamType<TestSimpleMessageParam, TestMessageParam1>("TestRequest1", "RespToReq1");
				m_procPool.AddPairParamType<TestSimpleMessageParam, TestMessageParam2>("TestRequest2", "RespToReq2");

				m_bridge	= new TestClientBridge();
				m_procPool.SetBridge(m_bridge);

				m_bridge.m_poolCtrl.SetSendDelegate((packet) =>
				{
					var buffer				= packet.Pack();

					var request				= WebRequest.CreateHttp("http://127.0.0.1:8080/test/");
					request.Method			= "POST";
					request.ContentType		= "application/octet-stream";
					request.ContentLength	= buffer.Length;

					using (var stream = request.GetRequestStream())
					{
						stream.Write(buffer, 0, buffer.Length);
					}

					var response		= request.GetResponse() as HttpWebResponse;
					using (var stream = response.GetResponseStream())
					{
						var length		= (int)response.ContentLength;
						var readbuf		= new byte[length];
						stream.Read(readbuf, 0, length);

						m_bridge.m_poolCtrl.CallReceive(Packet.Unpack(readbuf));
					}
				});

				m_procPool.Start();
			}

			public void SendRequest1()
			{
				m_procPool.DoRequest<TestSimpleMessageParam, TestMessageParam1>("TestRequest1",
					(sendObj) =>
					{
						sendObj.SetParameter(new TestSimpleMessageParam());
					},
					(recvObj) =>
					{
						Console.Out.WriteLine("response 1 - message : {0}, data : {1}", recvObj.param.message, recvObj.param.data);
					});
			}

			public void SendRequest2()
			{
				m_procPool.DoRequest<TestSimpleMessageParam, TestMessageParam2>("TestRequest2",
					(sendObj) =>
					{
						sendObj.SetParameter(new TestSimpleMessageParam());
					},
					(recvObj) =>
					{
						string arrayStr = "";
						foreach (var i in recvObj.param.array)
							arrayStr += i + " ";

						Console.Out.WriteLine("response 2 - message : {0}, data : {1}", recvObj.param.message, arrayStr);
					});
			}
		}

		static void BakjeServerTest()
		{
			var random		= new Random();

			var server		= new Http.Server();
			var bridge		= server.CreateProcedurePoolBridge("/test/");
			var procpool	= new ServerProcedurePool();
			procpool.SetBridge(bridge);

			procpool.AddProcedure<TestSimpleMessageParam, TestMessageParam1>("TestRequest1", "RespToReq1", (recvObj, sendObj) =>
			{
				sendObj.SetParameter(new TestMessageParam1()
				{
					message	= "Server puts a random number",
					data	= random.Next()
				});
			});

			procpool.AddProcedure<TestSimpleMessageParam, TestMessageParam2>("TestRequest2", "RespToReq2", (recvObj, sendObj) =>
			{
				var randomArray	= new int[10];
				for (var i = 0; i < 10; i++)
					randomArray[i]	= i + 1;

				sendObj.SetParameter(new TestMessageParam2()
				{
					message	= "Server puts a incremental number sequence.",
					array	= randomArray
				});
			});

			procpool.Start();
			server.Start();

			Thread.Sleep(1000);

			var client	= new TestClient();
			client.SendRequest1();

			Thread.Sleep(1000);

			client.SendRequest2();

			Thread.Sleep(1000);

			client.SendRequest1();

			Thread.Sleep(1000);


			Console.Out.WriteLine("Press any key to close the server...");
			Console.ReadKey();
			server.Stop();
		}
	}
}

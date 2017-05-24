using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using BakjeProtocol;
using BakjeProtocol.Auth;
using System.Threading;

namespace BakjeShareServer
{
	class Program
	{
		static void Main(string[] args)
		{
			//BasicServerTest();
			//BasicPacketTest();
			//BakjeServerTest();
			BakjeSQLTest();
		}

		static void BakjeSQLTest()
		{
			var sql	= new SQL.SQLHelper();
			sql.SetupConnectionString("localhost", "bakjeserver", "bakje1234", "bakjedb");
			System.Action runSelect = () =>
				sql.RunSqlSession((bridge) =>
				{
					var cmd			= bridge.CreateCommand();
					cmd.CommandText	= "select iduser, email from user";
					var reader		= cmd.ExecuteReader();

					while (reader.Read())
					{
						Console.Out.WriteLine("{0} - {1}", reader.GetString("iduser"), reader.GetString("email"));
					}
					reader.Close();
				});

			runSelect();

			try
			{
				sql.RunSqlSessionWithTransaction((bridge) =>
				{
					var cmd			= bridge.CreateCommand();
					cmd.CommandText	= "insert into user(iduser, password, email, is_admin, is_blinded) values(@id, @pass, @email, false, false)";
					var param		= cmd.Parameters;
					param.AddWithValue("@id", "newuser1");
					param.AddWithValue("@pass", "12345");
					param.AddWithValue("@email", "emailto@bbb.aaa");

					cmd.ExecuteNonQuery();

					Console.Out.WriteLine("added some user data");

					return true;	// revert
				});
			}
			catch(Exception e)
			{
				Console.Out.WriteLine("...already added?");
			}
			
			runSelect();

			sql.RunSqlSession((bridge) =>
			{
				var cmd			= bridge.CreateCommand();
				cmd.CommandText	= "delete from user where iduser = @id";
				cmd.Parameters.AddWithValue("@id", "newuser1");

				cmd.ExecuteNonQuery();

				Console.Out.WriteLine("removed one");
			});


			Console.ReadKey();
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

		class TestLoginParam
		{
			public string userid;
			public string password;
		}

		class TestAuthResponseParam
		{
			public string authKey;
			public UserType userType;
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

			class TestAuthClient : BaseAuthClient
			{
				protected override void ClearLocalAuthToken()
				{
					
				}

				protected override void LoadLocalAuthToken(out string authKey, out UserType userType)
				{
					authKey		= null;
					userType	= UserType.Guest;
				}

				protected override void SaveLocalAuthToken()
				{
					
				}
			}

			ClientProcedurePool m_procPool;
			TestClientBridge	m_bridge;
			TestAuthClient		m_authClient;

			public TestClient()
			{
				m_procPool	= new ClientProcedurePool();
				m_procPool.AddPairParamType<TestSimpleMessageParam, TestMessageParam1>("TestRequest1", "RespToReq1");
				m_procPool.AddPairParamType<TestSimpleMessageParam, TestMessageParam2>("TestRequest2", "RespToReq2");
				m_procPool.AddPairParamType<TestLoginParam, TestAuthResponseParam>("TestLogin", "RespToLogin");
				
				m_bridge	= new TestClientBridge();
				m_procPool.SetBridge(m_bridge);

				m_authClient	= new TestAuthClient();
				m_procPool.SetAuthClientObject(m_authClient);
				

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

			public void SendLoginRequest(string userid, string password)
			{
				m_procPool.DoRequest<TestLoginParam, TestAuthResponseParam>("TestLogin", (sendObj) =>
					{
						sendObj.SetParameter(new TestLoginParam() { userid = userid, password = password });
					},
					(recvObj) =>
					{
						if (recvObj.header.code == Packet.Header.Code.OK)
						{
							var key	= recvObj.param.authKey;
							var ut	= recvObj.param.userType;
							m_authClient.SetNew(key, ut);

							Console.Out.WriteLine("login success - auth key : " + key);
						}
						else
						{
							Console.Out.WriteLine("login failed, continue with guest auth level");
						}
					});
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
						if (recvObj.header.code == Packet.Header.Code.AuthNeeded)
						{
							Console.Out.WriteLine("response 1 - auth needed");
						}
						else
						{
							Console.Out.WriteLine("response 1 - message : {0}, data : {1}", recvObj.param.message, recvObj.param.data);
						}
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
						if (recvObj.header.code == Packet.Header.Code.AuthNeeded)
						{
							Console.Out.WriteLine("response 2 - auth needed");
						}
						else
						{
							string arrayStr = "";
							foreach (var i in recvObj.param.array)
								arrayStr += i + " ";

							Console.Out.WriteLine("response 2 - message : {0}, data : {1}", recvObj.param.message, arrayStr);
						}
					});
			}
		}

		class TestAuthServer : BaseAuthServer
		{
			class Entry
			{
				public string userid;
				public string authkey;
				public UserType usertype;
			}

			List<Entry> m_testdb;

			public TestAuthServer()
			{
				m_testdb	= new List<Entry>();
			}


			protected override string GenerateAuthKey(string userid)
			{
				return userid + ":" + DateTime.Now.ToString();
			}

			protected override string QueryAuthKey(string userid)
			{
				foreach (var entry in m_testdb)
				{
					if (entry.userid == userid)
						return entry.authkey;
				}
				return null;
			}

			protected override void QuerySetAuthInfo(string userid, string authkey, UserType usertype)
			{
				foreach(var entry in m_testdb)
				{
					if (entry.userid == userid)
					{
						entry.authkey	= authkey;
						entry.usertype	= usertype;
						return;
					}
				}
				m_testdb.Add(new Entry() { userid = userid, authkey = authkey, usertype = usertype });
			}

			protected override string QueryUserID(string authkey)
			{
				foreach (var entry in m_testdb)
				{
					if (entry.authkey == authkey)
						return entry.userid;
				}
				return null;
			}

			protected override UserType QueryUserType(string userid)
			{
				foreach (var entry in m_testdb)
				{
					if (entry.userid == userid)
						return entry.usertype;
				}
				return UserType.Guest;
			}
		}

		static void BakjeServerTest()
		{
			var random		= new Random();

			var server		= new Http.Server();
			var bridge		= server.CreateProcedurePoolBridge("/test/");
			var procpool	= new ServerProcedurePool();
			var authserver	= new TestAuthServer();
			procpool.SetBridge(bridge);
			procpool.SetAuthServerObj(authserver);

			procpool.AddProcedure<TestLoginParam, TestAuthResponseParam>("TestLogin", "RespToLogin", UserType.Guest, (recvObj, sendObj) =>
			{
				var param	= recvObj.param;
				var newauth	= (string)null;
				var usertype= UserType.Guest;
				if (param.userid == "user" && param.password == "userpass")
				{
					usertype	= UserType.Registered;
					newauth		= authserver.SetupNewAuthKey(param.userid, usertype);
				}
				else if (param.userid == "admin" && param.password == "adminpass")
				{
					usertype	= UserType.Administrator;
					newauth		= authserver.SetupNewAuthKey(param.userid, usertype);
				}

				sendObj.SetParameter(new TestAuthResponseParam() { authKey = newauth, userType = usertype });
				sendObj.header.code		= (newauth == null) ? Packet.Header.Code.ClientSideError : Packet.Header.Code.OK;
			});

			procpool.AddProcedure<TestSimpleMessageParam, TestMessageParam1>("TestRequest1", "RespToReq1", UserType.Registered, (recvObj, sendObj) =>
			{
				sendObj.SetParameter(new TestMessageParam1()
				{
					message	= "Server puts a random number",
					data	= random.Next()
				});
			});

			procpool.AddProcedure<TestSimpleMessageParam, TestMessageParam2>("TestRequest2", "RespToReq2", UserType.Administrator, (recvObj, sendObj) =>
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
			client.SendLoginRequest("user", "userpass");
			//client.SendLoginRequest("admin", "adminpass");
			//client.SendLoginRequest("admin", "aaaaasssssdddd");

			Thread.Sleep(1000);
			
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

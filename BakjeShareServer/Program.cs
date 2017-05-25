using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using BakjeProtocol;
using BakjeProtocol.Auth;
using BakjeProtocol.Parameters;
using System.Threading;

namespace BakjeShareServer
{
	class Program
	{
		static void Main(string[] args)
		{
			RealServerTest();
		}

		static void RealServerTest()
		{
			var server		= new Http.Server();
			var sqlHelper	= new SQL.SQLHelper();
			sqlHelper.SetupConnectionString("localhost", "bakjeserver", "bakje1234", "bakjedb");
			var authserver	= new SQL.SQLAuthServer(sqlHelper);
			var authPP		= new Procedures.Auth("/auth/", server, authserver, sqlHelper);

			server.Start();

			Thread.Sleep(1000);
			
			var client		= new TestClient();

			client.SendCheckAuth();
			client.SendLoginRequest("defaultuser", "blaaah");
			client.SendCheckAuth();
			client.SendNewuser("newuser", "pass1234", "newuser@email.com");
			client.SendLoginRequest("newuser", "pass1234");
			client.SendDeleteUser();

			Console.ReadKey();
			server.Stop();
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

			ClientProcedurePool m_authPP;
			TestClientBridge	m_authPPbridge;
			TestAuthClient		m_authClient;

			public TestClient()
			{
				m_authPP		= new ClientProcedurePool();
				m_authPP.AddPairParamType<ReqLogin, RespLogin>("ReqLogin", "RespLogin");
				m_authPP.AddPairParamType<EmptyParam, EmptyParam>("ReqCheckAuth", "RespCheckAuth");
				m_authPP.AddPairParamType<ReqNewUser, RespNewUser>("ReqNewUser", "RespNewUser");
				m_authPP.AddPairParamType<EmptyParam, EmptyParam>("ReqDeleteUser", "RespDeleteUser");

				m_authPPbridge	= new TestClientBridge();
				m_authPP.SetBridge(m_authPPbridge);

				m_authClient	= new TestAuthClient();
				m_authPP.SetAuthClientObject(m_authClient);
				

				m_authPPbridge.m_poolCtrl.SetSendDelegate((packet) => PacketSend(packet, "/auth/", m_authPPbridge));

				m_authPP.Start();
			}

			static void PacketSend(Packet packet, string suburl, TestClientBridge bridge)
			{
				var buffer				= packet.Pack();

				var request				= WebRequest.CreateHttp("http://127.0.0.1:8080" + suburl);
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

					bridge.m_poolCtrl.CallReceive(Packet.Unpack(readbuf));
				}
			}

			public void SendCheckAuth()
			{
				m_authPP.DoRequest<EmptyParam, EmptyParam>("ReqCheckAuth", (sendObj) =>
				{
					
				},
				(recvObj) =>
				{
					if (recvObj.header.code == Packet.Header.Code.OK)
					{
						Console.Out.WriteLine("auth key valid");
					}
					else
					{
						Console.Out.WriteLine("auth key invalid - need login");
					}
				});
			}

			public void SendLoginRequest(string userid, string password)
			{
				m_authPP.DoRequest<ReqLogin, RespLogin>("ReqLogin", (sendObj) =>
					{
						sendObj.SetParameter(new ReqLogin() { userid = userid, password = password });
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

			public void SendNewuser(string userid, string password, string email)
			{
				m_authPP.DoRequest<ReqNewUser, RespNewUser>("ReqNewUser", (sendObj) =>
				{
					sendObj.SetParameter(new ReqNewUser() { userid = userid, password = password, email = email });
				},
				(recvObj) =>
				{
					if (recvObj.header.code == Packet.Header.Code.OK)
					{
						Console.Out.WriteLine("user registration success");
					}
					else if (recvObj.param.status == RespNewUser.Status.DuplicatedID)
					{
						Console.Out.WriteLine("duplicated id");
					}
					else if (recvObj.param.status == RespNewUser.Status.AlreadyRegisteredEmail)
					{
						Console.Out.WriteLine("already registered email");
					}
				});
			}

			public void SendDeleteUser()
			{
				m_authPP.DoRequest<EmptyParam, EmptyParam>("ReqDeleteUser", (sendObj) =>
				{

				},
				(recvObj) =>
				{
					Console.Out.WriteLine("user deleted");
					m_authClient.Clear();
				});
			}
		}
	}
}

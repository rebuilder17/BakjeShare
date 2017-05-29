﻿using System;
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
			var postPP		= new Procedures.Posting("/posting/", server, authserver, sqlHelper);
			var reportPP	= new Procedures.Report("/report/", server, authserver, sqlHelper);

			server.Start();

			Thread.Sleep(1000);
			
			var client		= new TestClient();

			//client.SendCheckAuth();
			//client.SendLoginRequest("defaultuser", "blaaah");
			//client.SendCheckAuth();
			//client.SendNewuser("newuser", "pass1234", "newuser@email.com");
			//client.SendLoginRequest("newuser", "pass1234");
			//client.SendDeleteUser();

			//client.SendNewuser("user1", "user1", "user1@test.user");
			//client.SendNewuser("user2", "user2", "user2@test.user");

			//client.SendLoginRequest("user1", "user1");
			//var postid = client.SendNewPost("test posting by user1 (01)", "테스트 포스팅입니다.", null, null);
			//client.SendNewPost("test posting by user1 (02)", "테스트 포스팅입니다. 222222", null, null);
			//client.SendNewPost("으아아아아아아악", "컄", null, null);
			//client.SendAddTag(postid, "태그1");
			//client.SendAddTag(postid, "태그2");
			//client.SendAddTag(postid, "테스트태그");

			//client.SendLoginRequest("user2", "user2");
			//client.SendAddTag(postid, "다른유저태그");

			client.SendLoginRequest("user1", "user1");
			var postings	= client.SendLookupPosting(null, null, null, null, 0, 20);
			foreach (var entry in postings.entries)
			{
				Console.Out.WriteLine("{0} : {1} by {2} (date : {3})", entry.postID, entry.title, entry.author, entry.postingTime);
			}
			Console.Out.WriteLine("page {0} of {1}", postings.currentPage + 1, postings.totalPage);
			
			Console.Out.WriteLine("any key to close...");
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

			TestAuthClient		m_authClient;

			ClientProcedurePool m_authPP;
			ClientProcedurePool	m_postPP;
			ClientProcedurePool	m_reptPP;



			public TestClient()
			{
				m_authClient	= new TestAuthClient();

				m_authPP		= new ClientProcedurePool();
				m_authPP.AddPairParamType<ReqLogin, RespLogin>("ReqLogin", "RespLogin");
				m_authPP.AddPairParamType<EmptyParam, EmptyParam>("ReqCheckAuth", "RespCheckAuth");
				m_authPP.AddPairParamType<ReqNewUser, RespNewUser>("ReqNewUser", "RespNewUser");
				m_authPP.AddPairParamType<EmptyParam, EmptyParam>("ReqDeleteUser", "RespDeleteUser");

				var authBridge	= new TestClientBridge();
				m_authPP.SetBridge(authBridge);
				m_authPP.SetAuthClientObject(m_authClient);

				authBridge.m_poolCtrl.SetSendDelegate((packet) => PacketSend(packet, "/auth/", authBridge));
				m_authPP.Start();
				//

				m_postPP		= new ClientProcedurePool();
				m_postPP.AddPairParamType<ReqLookupPosting, RespLookupPosting>("ReqLookupPosting", "RespLookupPosting");
				m_postPP.AddPairParamType<ReqShowPosting, RespShowPosting>("ReqShowPosting", "RespShowPosting");
				m_postPP.AddPairParamType<ReqNewPosting, RespPostingModify>("ReqNewPosting", "RespNewPosting");
				m_postPP.AddPairParamType<ReqDeletePosting, RespDeletePosting>("ReqDeletePosting", "RespDeletePosting");
				m_postPP.AddPairParamType<ReqAddTag, RespAddTag>("ReqAddTag", "RespAddTag");
				m_postPP.AddPairParamType<ReqDeleteTag, RespDeleteTag>("ReqDeleteTag", "RespDeleteTag");

				var postBridge	= new TestClientBridge();
				m_postPP.SetBridge(postBridge);
				m_postPP.SetAuthClientObject(m_authClient);

				postBridge.m_poolCtrl.SetSendDelegate((packet) => PacketSend(packet, "/posting/", postBridge));
				m_postPP.Start();
				//

				m_reptPP		= new ClientProcedurePool();
				m_reptPP.AddPairParamType<ReqFileReport, RespFileReport>("ReqFileReport", "RespFileReport");
				m_reptPP.AddPairParamType<ReqLookupReport, RespLookupReport>("ReqLookupReport", "RespLookupReport");
				m_reptPP.AddPairParamType<ReqShowReport, RespShowReport>("ReqShowReport", "RespShowReport");
				m_reptPP.AddPairParamType<ReqCloseReport, RespCloseReport>("ReqCloseReport", "RespCloseReport");

				var reptBridge	= new TestClientBridge();
				m_reptPP.SetBridge(reptBridge);
				m_reptPP.SetAuthClientObject(m_authClient);

				reptBridge.m_poolCtrl.SetSendDelegate((packet) => PacketSend(packet, "/report/", reptBridge));
				m_reptPP.Start();
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
			//

			public int SendNewPost(string title, string description, string sourceURL, IList<byte[]> dataList = null)
			{
				var postID	= -1;

				m_postPP.DoRequest<ReqNewPosting, RespPostingModify>("ReqNewPosting",
					(send) =>
					{
						send.SetParameter(new ReqNewPosting
						{
							title	= title,
							desc	= description,
							sourceURL	= sourceURL,
						});

						if (dataList != null)
						{
							foreach(var data in dataList)
							{
								send.AddBinaryData(data);
							}
						}
					},
					(recv) =>
					{
						postID = recv.param.postID;
					});

				return postID;
			}

			public void SendDeletePost(int postID)
			{
				m_postPP.DoRequest<ReqDeletePosting, RespDeletePosting>("ReqDeletePosting",
					(send) =>
					{
						send.SetParameter(new ReqDeletePosting { postID = postID });
					},
					(recv) =>
					{

					});
			}

			public void SendAddTag(int postID, string tag)
			{
				m_postPP.DoRequest<ReqAddTag, RespAddTag>("ReqAddTag",
					(send) =>
					{
						send.SetParameter(new ReqAddTag { postID = postID, tagname = tag });
					},
					(recv) =>
					{

					});
			}

			public void SendDeleteTag(int postID, string tag)
			{
				m_postPP.DoRequest<ReqDeleteTag, RespDeleteTag>("ReqDeleteTag",
					(send) =>
					{
						send.SetParameter(new ReqDeleteTag { postID = postID, tagname = tag });
					}, 
					(recv) =>
					{

					});
			}

			public RespLookupPosting SendLookupPosting(string kwtitle, string kwdesc, string kwtag, string kwuser, int page = 0, int rowperpage = 20)
			{
				RespLookupPosting result = null;

				m_postPP.DoRequest<ReqLookupPosting, RespLookupPosting>("ReqLookupPosting",
					(send) =>
					{
						send.SetParameter(new ReqLookupPosting
						{
							keyword_title	= kwtitle?.Split(),
							keyword_desc	= kwdesc?.Split(),
							keyword_tag		= kwtag,
							keyword_user	= kwuser,
							page			= page,
							rowPerPage		= rowperpage,
						});
					},
					(recv) =>
					{
						result	= recv.param;
					});

				return result;
			}
		}
	}
}

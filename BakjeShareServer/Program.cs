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
			RetrieveExternalIP();
			InitializeServer();
		}

		static void RetrieveExternalIP()
		{
			Console.WriteLine("retrieve external IP via http://icanhazip.com/ ....");
			try
			{
				var request		= WebRequest.CreateHttp("http://icanhazip.com/");
				request.Timeout	= 5000;
				request.Method	= "GET";
				using (var response	= request.GetResponse())
				{
					using (var stream	= response.GetResponseStream())
					{
						var len			= response.ContentLength;
						var buffer		= new byte[len];
						stream.Read(buffer, 0, (int)len);

						Console.WriteLine("Server External IP : {0}", Encoding.ASCII.GetString(buffer));
					}
				}
			}
			catch(WebException)
			{
				Console.WriteLine("cannot connect to http://icanhazip.com/ . You should get your external ip manually.");
			}
		}

		static void InitializeServer()
		{
			var server		= new Http.Server();
			var sqlHelper	= new SQL.SQLHelper();
			sqlHelper.SetupConnectionString("localhost", "bakjeserver", "bakje1234", "bakjedb");
			var authserver	= new SQL.SQLAuthServer(sqlHelper);
			var authPP		= new Procedures.Auth("/auth/", server, authserver, sqlHelper);
			var postPP		= new Procedures.Posting("/posting/", server, authserver, sqlHelper);
			var reportPP	= new Procedures.Report("/report/", server, authserver, sqlHelper);
			var noticePP	= new Procedures.Notice("/notice/", server, authserver, sqlHelper);
			var userPP		= new Procedures.User("/user/", server, authserver, sqlHelper);

			server.Start();

			Console.Out.WriteLine("Server started. any key to close...");
			Console.ReadKey();
			server.Stop();
		}

		//static void RealServerTest()
		//{
		//	Thread.Sleep(1000);
			
		//	var client		= new TestClient();

		//	//client.SendCheckAuth();
		//	//client.SendLoginRequest("defaultuser", "blaaah");
		//	//client.SendCheckAuth();
		//	//client.SendNewuser("newuser", "pass1234", "newuser@email.com");
		//	//client.SendLoginRequest("newuser", "pass1234");
		//	//client.SendDeleteUser();

		//	//client.SendNewuser("user1", "user1", "user1@test.user");
		//	//client.SendNewuser("user2", "user2", "user2@test.user");

		//	//client.SendLoginRequest("user1", "user1");
		//	//var postid = client.SendNewPost("test posting by user1 (01)", "테스트 포스팅입니다.", null, null);
		//	//client.SendNewPost("test posting by user1 (02)", "테스트 포스팅입니다. 222222", null, null);
		//	//client.SendNewPost("으아아아아아아악", "컄", null, null);
		//	//client.SendAddTag(postid, "태그1");
		//	//client.SendAddTag(postid, "태그2");
		//	//client.SendAddTag(postid, "테스트태그");

		//	//client.SendLoginRequest("user2", "user2");
		//	//client.SendAddTag(postid, "다른유저태그");

		//	//client.SendLoginRequest("user1", "user1");
		//	//var postings	= client.SendLookupPosting(null, null, null, null, 0, 20);
		//	//foreach (var entry in postings.entries)
		//	//{
		//	//	Console.Out.WriteLine("{0} : {1} by {2} (date : {3})", entry.postID, entry.title, entry.author, entry.postingTime);
		//	//}
		//	//Console.Out.WriteLine("page {0} of {1}", postings.currentPage + 1, postings.totalPage);

		//	//client.SendLoginRequest("user1", "user1");
		//	//client.SendFileReport_Bug("버그좀잡아주세요;", "참내;;;");
		//	//client.SendLoginRequest("user2", "user2");
		//	//client.SendFileReport_User("얘 리폿좀요", "욕해요", "user1", ReqFileReport.UserReportReason.Etc);
		//	//client.SendLoginRequest("defaultuser", "blaaah");
		//	//client.SendFileReport_Posting("포스팅 극혐", "내려주세요", 11, ReqFileReport.PostReportReason.Etc);
		//	//client.SendLookupReport();

		//	//client.SendLoginRequest("admin", "admin");
		//	//client.SendCloseReport(5);
		//	//var reports = client.SendLookupReport();
		//	//foreach(var entry in reports.entries)
		//	//{
		//	//	Console.Out.WriteLine("{0} : {1} by {2} type : {3}", entry.reportID, entry.shortdesc, entry.reporterID, entry.type);
		//	//}

		//	//var report = client.SendShowReport(5);
		//	//Console.Out.WriteLine("title : {0}", report.shortdesc);
		//	//switch(report.type)
		//	//{
		//	//	case ReqFileReport.Type.Bug:
		//	//		Console.Out.WriteLine("bug report");
		//	//		break;
		//	//	case ReqFileReport.Type.Posting:
		//	//		Console.Out.WriteLine("reports posting : {0}", report.repPostingID);
		//	//		break;
		//	//	case ReqFileReport.Type.User:
		//	//		Console.Out.WriteLine("reports user : {0}", report.repUserID);
		//	//		break;
		//	//}
		//	//Console.Out.WriteLine("desc : {0}", report.longdesc);

		//	//client.SendLoginRequest("admin", "admin");
		//	//client.SendPostNotice("임시점검", "안해요");
		//	//client.SendDeleteNotice(2);
		//	//var notilist = client.SendLookupNotice();
		//	//foreach(var entry in notilist.entries)
		//	//{
		//	//	Console.Out.WriteLine("{0} {1} (time : {2}", entry.noticeID, entry.title, entry.datetime);
		//	//}

		//	//var notice = client.SendShowNotice(3);
		//	//Console.Out.WriteLine("title : {0}\ndate : {1}\ndetail : {2}", notice.title, notice.datetime, notice.desc);

		//	//client.SendLoginRequest("user2", "user2");
		//	//client.SendNewPost("비밀글", "캬", "", true);
		//	client.SendLoginRequest("admin", "admin");
		//	client.SendBlindUser("user2", false);
		//	client.SendBlindUser("user1", false);
		//	client.SendBlindPost(9, true);
		//	var postings	= client.SendLookupPosting(null, null, null, null, 0, 20);
		//	foreach (var entry in postings.entries)
		//	{
		//		Console.Out.WriteLine("{0} : {1} by {2} (date : {3}) {4}", entry.postID, entry.title, entry.author, entry.postingTime, entry.isBlinded? "(blinded)" : "");
		//	}
		//	Console.Out.WriteLine("page {0} of {1}", postings.currentPage + 1, postings.totalPage);

		//	client.SendLoginRequest("user1", "user1");
		//	postings	= client.SendLookupPosting(null, null, null, null, 0, 20);
		//	foreach (var entry in postings.entries)
		//	{
		//		Console.Out.WriteLine("{0} : {1} by {2} (date : {3}) {4}", entry.postID, entry.title, entry.author, entry.postingTime, entry.isBlinded ? "(blinded)" : "");
		//	}
		//	Console.Out.WriteLine("page {0} of {1}", postings.currentPage + 1, postings.totalPage);

		//	Console.Out.WriteLine("any key to close...");
		//	Console.ReadKey();
		//	server.Stop();
		//}

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

				protected override void SaveLocalAuthToken(string key, UserType ut)
				{

				}
			}

			TestAuthClient		m_authClient;

			ClientProcedurePool m_authPP;
			ClientProcedurePool	m_userPP;
			ClientProcedurePool	m_postPP;
			ClientProcedurePool	m_reptPP;
			ClientProcedurePool	m_notiPP;


			public TestClient()
			{
				m_authClient	= new TestAuthClient();

				m_authPP		= new ClientProcedurePool();
				m_authPP.AddPairParamType<ReqLogin, RespLogin>("ReqLogin", "RespLogin");
				m_authPP.AddPairParamType<EmptyParam, EmptyParam>("ReqCheckAuth", "RespCheckAuth");

				var authBridge	= new TestClientBridge();
				m_authPP.SetBridge(authBridge);
				m_authPP.SetAuthClientObject(m_authClient);

				authBridge.m_poolCtrl.SetSendDelegate((packet) => PacketSend(packet, "/auth/", authBridge));
				m_authPP.Start();
				//

				m_userPP		= new ClientProcedurePool();
				m_userPP.AddPairParamType<ReqNewUser, RespNewUser>("ReqNewUser", "RespNewUser");
				m_userPP.AddPairParamType<EmptyParam, EmptyParam>("ReqDeleteUser", "RespDeleteUser");
				m_userPP.AddPairParamType<ReqBlindUser, EmptyParam>("ReqBlindUser", "RespBlindUser");
				m_userPP.AddPairParamType<ReqUserInfo, RespUserInfo>("ReqUserInfo", "RespUserInfo");

				var userBridge	= new TestClientBridge();
				m_userPP.SetBridge(userBridge);
				m_userPP.SetAuthClientObject(m_authClient);

				userBridge.m_poolCtrl.SetSendDelegate((packet) => PacketSend(packet, "/user/", userBridge));
				m_userPP.Start();
				//

				m_postPP		= new ClientProcedurePool();
				m_postPP.AddPairParamType<ReqLookupPosting, RespLookupPosting>("ReqLookupPosting", "RespLookupPosting");
				m_postPP.AddPairParamType<ReqShowPosting, RespShowPosting>("ReqShowPosting", "RespShowPosting");
				m_postPP.AddPairParamType<ReqNewPosting, RespPostingModify>("ReqNewPosting", "RespNewPosting");
				m_postPP.AddPairParamType<ReqDeletePosting, RespDeletePosting>("ReqDeletePosting", "RespDeletePosting");
				m_postPP.AddPairParamType<ReqBlindPosting, EmptyParam>("ReqBlindPosting", "RespBlindPosting");
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
				//

				m_notiPP		= new ClientProcedurePool();
				m_notiPP.AddPairParamType<ReqPostNotice, EmptyParam>("ReqPostNotice", "RespPostNotice");
				m_notiPP.AddPairParamType<ReqDeleteNotice, EmptyParam>("ReqDeleteNotice", "RespDeleteNotice");
				m_notiPP.AddPairParamType<ReqLookupNotice, RespLookupNotice>("ReqLookupNotice", "RespLookupNotice");
				m_notiPP.AddPairParamType<ReqShowNotice, RespShowNotice>("ReqShowNotice", "RespShowNotice");

				var notiBridge	= new TestClientBridge();
				m_notiPP.SetBridge(notiBridge);
				m_notiPP.SetAuthClientObject(m_authClient);

				notiBridge.m_poolCtrl.SetSendDelegate((packet) => PacketSend(packet, "/notice/", notiBridge));
				m_notiPP.Start();
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
				
				try
				{
					var response		= request.GetResponse() as HttpWebResponse;
					using (var stream = response.GetResponseStream())
					{
						var length		= (int)response.ContentLength;
						var readbuf		= new byte[length];
						stream.Read(readbuf, 0, length);

						bridge.m_poolCtrl.CallReceive(Packet.Unpack(readbuf));
					}
				}
				catch(WebException e)							// 웹 연결이 에러 코드를 주면 Server Error로 처리한다.
				{
					bridge.m_poolCtrl.CallReceiveServerError(packet.header.messageType);
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
				m_userPP.DoRequest<ReqNewUser, RespNewUser>("ReqNewUser", (sendObj) =>
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
				m_userPP.DoRequest<EmptyParam, EmptyParam>("ReqDeleteUser", (sendObj) =>
				{

				},
				(recvObj) =>
				{
					Console.Out.WriteLine("user deleted");
					m_authClient.Clear();
				});
			}

			public void SendBlindUser(string userID, bool setBlind)
			{
				m_userPP.DoRequest<ReqBlindUser, EmptyParam>("ReqBlindUser",
					(send) =>
					{
						send.SetParameter(new ReqBlindUser { userID = userID, setBlind = setBlind });
					},
					(recv) =>
					{

					});
			}

			public RespUserInfo SendShowUserInfo(string userID)
			{
				RespUserInfo result = null;

				m_userPP.DoRequest<ReqUserInfo, RespUserInfo>("ReqUserInfo",
					(send) =>
					{
						send.SetParameter(new ReqUserInfo { userID = userID });
					},
					(recv) =>
					{
						result	= recv.param;
					});
				return result;
			}
			//

			public int SendNewPost(string title, string description, string sourceURL, bool isPrivate, IList<byte[]> dataList = null)
			{
				var postID	= -1;

				m_postPP.DoRequest<ReqNewPosting, RespPostingModify>("ReqNewPosting",
					(send) =>
					{
						send.SetParameter(new ReqNewPosting
						{
							title		= title,
							desc		= description,
							sourceURL	= sourceURL,
							isPrivate	= isPrivate,
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

			public void SendBlindPost(int postID, bool setblind)
			{
				m_postPP.DoRequest<ReqBlindPosting, EmptyParam>("ReqBlindPosting",
					(send) =>
					{
						send.SetParameter(new ReqBlindPosting { postID = postID, setBlind = setblind });
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

			public void SendFileReport_Posting(string shortdesc, string longdesc, int postid, ReqFileReport.PostReportReason reason)
			{
				m_reptPP.DoRequest<ReqFileReport, RespFileReport>("ReqFileReport",
					(send) =>
					{
						send.SetParameter(new ReqFileReport
						{
							type				= ReqFileReport.Type.Posting,
							shortdesc			= shortdesc,
							longdesc			= longdesc,
							reportingPostID		= postid,
							postReportReason	= reason,
						});
					},
					(recv) =>
					{

					});
			}

			public void SendFileReport_User(string shortdesc, string longdesc, string reportID, ReqFileReport.UserReportReason reason)
			{
				m_reptPP.DoRequest<ReqFileReport, RespFileReport>("ReqFileReport",
					(send) =>
					{
						send.SetParameter(new ReqFileReport
						{
							type				= ReqFileReport.Type.User,
							shortdesc			= shortdesc,
							longdesc			= longdesc,
							reportingUserID		= reportID,
							userReportReason	= reason,
						});
					},
					(recv) =>
					{

					});
			}

			public void SendFileReport_Bug(string shortdesc, string longdesc)
			{
				m_reptPP.DoRequest<ReqFileReport, RespFileReport>("ReqFileReport",
					(send) =>
					{
						send.SetParameter(new ReqFileReport
						{
							type				= ReqFileReport.Type.Bug,
							shortdesc			= shortdesc,
							longdesc			= longdesc,
						});
					},
					(recv) =>
					{

					});
			}

			public RespLookupReport SendLookupReport()
			{
				RespLookupReport result = null;

				m_reptPP.DoRequest<ReqLookupReport, RespLookupReport>("ReqLookupReport",
					(send) =>
					{
						send.SetParameter(new ReqLookupReport
						{
							page = 0,
							rowPerPage = 20,
						});
					},
					(recv) =>
					{
						result = recv.param;
					});

				return result;
			}

			public RespShowReport SendShowReport(int reportid)
			{
				RespShowReport result = null;

				m_reptPP.DoRequest<ReqShowReport, RespShowReport>("ReqShowReport",
					(send) =>
					{
						send.SetParameter(new ReqShowReport { reportID = reportid });
					},
					(recv) =>
					{
						result = recv.param;
					});

				return result;
			}

			public void SendCloseReport(int reportid)
			{
				m_reptPP.DoRequest<ReqCloseReport, RespCloseReport>("ReqCloseReport",
					(send) =>
					{
						send.SetParameter(new ReqCloseReport { postID = reportid });
					},
					(recv) =>
					{

					});
			}

			public void SendPostNotice(string title, string desc)
			{
				m_notiPP.DoRequest<ReqPostNotice, EmptyParam>("ReqPostNotice",
					(send) =>
					{
						send.SetParameter(new ReqPostNotice { title = title, desc = desc });
					},
					(recv) =>
					{

					});
			}

			public void SendDeleteNotice(int noticeID)
			{
				m_notiPP.DoRequest<ReqDeleteNotice, EmptyParam>("ReqDeleteNotice",
					(send) =>
					{
						send.SetParameter(new ReqDeleteNotice { noticeID = noticeID });
					},
					(recv) =>
					{

					});
			}

			public RespLookupNotice SendLookupNotice()
			{
				RespLookupNotice result = null;

				m_notiPP.DoRequest<ReqLookupNotice, RespLookupNotice>("ReqLookupNotice",
					(send) =>
					{
						send.SetParameter(new ReqLookupNotice { page = 0, rowperpage = 20 });
					},
					(recv) =>
					{
						result	= recv.param;
					});

				return result;
			}

			public RespShowNotice SendShowNotice(int noticeid)
			{
				RespShowNotice result = null;

				m_notiPP.DoRequest<ReqShowNotice, RespShowNotice>("ReqShowNotice",
					(send) =>
					{
						send.SetParameter(new ReqShowNotice { noticeID = noticeid });
					},
					(recv) =>
					{
						result = recv.param;
					});

				return result;
			}
		}
	}
}

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BakjeProtocol.Parameters;
using BakjeProtocol;

namespace BakjeClient.Engine
{
	public partial class ClientEngine
	{
		public enum AuthCheckResult
		{
			None,

			OK,
			CannotConnect,
			LoginNeeded,
		}

		public interface IAuth
		{
			AuthCheckResult CheckAuth();
			void ClearAuth();
			RespLogin.Status RequestLogin(string userid, string password);
		}

		protected class Auth : AutoProcedurePool, IAuth
		{
			public Auth(ClientEngine engine) : base(engine)
			{
			}

			public override string subURL => "/auth/";
			
			protected override void InitParamPairs()
			{
				AddParamPair<ReqLogin, RespLogin>();
				AddParamPair<EmptyParam, EmptyParam>("ReqCheckAuth", "RespCheckAuth");
			}

			public void ClearAuth()
			{
				authClient.Clear();
			}

			public AuthCheckResult CheckAuth()
			{
				var result = AuthCheckResult.None;

				DoRequest<EmptyParam, EmptyParam>("ReqCheckAuth",
					(send) =>
					{

					},
					(recv) =>
					{
						switch(recv.header.code)
						{
							case BakjeProtocol.Packet.Header.Code.OK:
								result	= AuthCheckResult.OK;
								break;

							case BakjeProtocol.Packet.Header.Code.AuthNeeded:
								result	= AuthCheckResult.LoginNeeded;
								break;

							case BakjeProtocol.Packet.Header.Code.ServerSideError:
							default:
								result	= AuthCheckResult.CannotConnect;
								break;
						}
					});

				return result;
			}

			public RespLogin.Status RequestLogin(string userid, string password)
			{
				var result = RespLogin.Status.NoUserInfo;

				DoRequest<ReqLogin, RespLogin>((sendObj) =>
				{
					sendObj.SetParameter(new ReqLogin() { userid = userid, password = password });
				},
					(recvObj) =>
					{
						result	= recvObj.param.status;

						if (recvObj.header.code == Packet.Header.Code.OK)			// 로그인 성공
						{
							var key	= recvObj.param.authKey;
							var ut	= recvObj.param.userType;
							authClient.SetNew(key, ut);
						}
					});

				return result;
			}
		}
	}
}

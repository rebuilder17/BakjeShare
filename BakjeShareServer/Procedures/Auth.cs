using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BakjeProtocol;
using BakjeProtocol.Auth;
using BakjeShareServer.Http;
using BakjeShareServer.SQL;
using BakjeProtocol.Parameters;

namespace BakjeShareServer.Procedures
{
	public class Auth : BaseProcedureSet
	{
		public Auth(string suburl, Server server, BaseAuthServer authServer, SQLHelper sqlhelper) : base(suburl, server, authServer, sqlhelper)
		{
			
		}

		protected override void Initialize()
		{
			// 로그인
			procedurePool.AddProcedure<ReqLogin, RespLogin>("ReqLogin", "RespLogin", UserType.Guest, (recv, send) =>
			{
				var id		= recv.param.userid;
				var pass	= recv.param.password;

				sqlHelper.RunSqlSession((sql) =>
				{
					var sendParam	= new RespLogin();

					var cmd	= sql.CreateCommand();
					cmd.CommandText	= "select iduser, password, is_admin, is_blinded from user where iduser = @id";
					cmd.Parameters.AddWithValue("@id", id);

					var reader	= cmd.ExecuteReader();
					if (!reader.HasRows)											// 맞는 유저가 없는 경우
					{
						send.header.code	= Packet.Header.Code.ClientSideError;
						sendParam.status	= RespLogin.Status.WrongPassword;
					}
					else
					{
						reader.Read();

						if (reader.GetString("password") != pass)					// 패스워드 틀린 경우
						{
							send.header.code	= Packet.Header.Code.ClientSideError;
							sendParam.status	= RespLogin.Status.WrongPassword;
						}
						else if (reader.GetBoolean("is_blinded"))					// 유저 차단된 경우
						{
							send.header.code	= Packet.Header.Code.ClientSideError;
							sendParam.status	= RespLogin.Status.BlindedUser;
						}
						else
						{															// 다 통과했으면 로그인시켜준다...
							var isadmin			= reader.GetBoolean("is_admin");
							var utype			= isadmin? UserType.Administrator : UserType.Registered;
							var newkey			= authServer.SetupNewAuthKey(id, utype);	// 새 authkey 생성해서 설정
							
							send.header.code	= Packet.Header.Code.OK;
							sendParam.status	= RespLogin.Status.OK;
							sendParam.authKey	= newkey;
							sendParam.userType	= utype;
						}

						reader.Close();
					}

					send.SetParameter(sendParam);
				});
			});

			// 현재 Auth 유효한지 체크
			procedurePool.AddProcedure<EmptyParam, EmptyParam>("ReqCheckAuth", "RespCheckAuth", UserType.Guest, (recv, send) =>
			{
				var validkey		= authServer.GetUserIDFromAuthKey(recv.header.authKey) != null;
				send.header.code	= validkey ? Packet.Header.Code.OK : Packet.Header.Code.AuthNeeded;
			});
		}
	}
}

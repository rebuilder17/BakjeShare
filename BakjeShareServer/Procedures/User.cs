using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BakjeProtocol.Auth;
using BakjeShareServer.Http;
using BakjeShareServer.SQL;
using BakjeProtocol.Parameters;

namespace BakjeShareServer.Procedures
{
	public class User : BaseProcedureSet
	{
		public User(string suburl, Server server, BaseAuthServer authServer, SQLHelper sqlhelper) : base(suburl, server, authServer, sqlhelper)
		{
		}

		protected override void Initialize()
		{
			// 유저 블라인드 세팅
			procedurePool.AddProcedure<ReqBlindUser, EmptyParam>("ReqBlindUser", "RespBlindUser", UserType.Administrator,
			(recv, send) =>
			{
				sqlHelper.RunSqlSessionWithTransaction((sql) =>
				{
					var cmd	= sql.CreateCommand();
					cmd.CommandText	= @"update user set is_blinded = @blind where iduser = @id";
					cmd.Parameters.AddWithValue("@id", recv.param.userID);
					cmd.Parameters.AddWithValue("@blind", recv.param.setBlind);
					cmd.ExecuteNonQuery();

					return true;
				});
			});

			// 유저 정보 얻기
			procedurePool.AddProcedure<ReqUserInfo, RespUserInfo>("ReqUserInfo", "RespUserInfo", UserType.Registered,
			(recv, send) =>
			{
				sqlHelper.RunSqlSession((sql) =>
				{
					var result	= new RespUserInfo();
					var userid	= recv.param.userID;

					var cmd	= sql.CreateCommand();
					cmd.CommandText	= @"select email, is_admin, is_blinded from user where iduser = @id";
					cmd.Parameters.AddWithValue("@id", userid);

					using (var reader = cmd.ExecuteReader())
					{
						reader.Read();

						result.userID		= userid;
						result.email		= reader.GetString("email");
						result.isBlinded	= reader.GetBoolean("is_blinded");
						result.isAdmin		= reader.GetBoolean("is_admin");

						reader.Close();
					}

					// 응답

					send.SetParameter(result);
					send.header.code	= BakjeProtocol.Packet.Header.Code.OK;
				});
			});
		}
	}
}

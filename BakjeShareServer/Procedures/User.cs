using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BakjeProtocol.Auth;
using BakjeShareServer.Http;
using BakjeShareServer.SQL;
using BakjeProtocol.Parameters;
using BakjeProtocol;

namespace BakjeShareServer.Procedures
{
	public class User : BaseProcedureSet
	{
		public User(string suburl, Server server, BaseAuthServer authServer, SQLHelper sqlhelper) : base(suburl, server, authServer, sqlhelper)
		{
		}

		protected override void Initialize()
		{
			// 새 유저 등록
			procedurePool.AddProcedure<ReqNewUser, RespNewUser>("ReqNewUser", "RespNewUser", UserType.Guest, (recv, send) =>
			{
				var id		= recv.param.userid;
				var pass	= recv.param.password;
				var email	= recv.param.email;

				sqlHelper.RunSqlSession((sql) =>
				{
					var sendParam	= new RespNewUser();

					var cmd	= sql.CreateCommand();
					cmd.CommandText	= @"select 
											(select count(*) from user where iduser = @id) as iddup, 
											(select count(*) from user where email = @email) as emaildup";
					cmd.Parameters.AddWithValue("@id", id);
					cmd.Parameters.AddWithValue("@email", email);

					var reader	= cmd.ExecuteReader();
					reader.Read();

					var dupUserID	= reader.GetInt32("iddup") != 0;
					var dupEmail	= reader.GetInt32("emaildup") != 0;

					reader.Close();

					if (dupUserID)			// ID 중복
					{
						send.header.code	= Packet.Header.Code.ClientSideError;
						sendParam.status	= RespNewUser.Status.DuplicatedID;
					}
					else if (dupEmail)		// Email중복
					{
						send.header.code	= Packet.Header.Code.ClientSideError;
						sendParam.status	= RespNewUser.Status.AlreadyRegisteredEmail;
					}
					else
					{						// 중복 없음.
						var addCmd			= sql.CreateCommand();
						addCmd.CommandText	= "insert into user(iduser, password, email, is_admin, is_blinded) values(@id, @pass, @email, false, false)";
						addCmd.Parameters.AddWithValue("@id", id);
						addCmd.Parameters.AddWithValue("@pass", pass);
						addCmd.Parameters.AddWithValue("@email", email);

						send.header.code	= Packet.Header.Code.OK;
						sendParam.status	= RespNewUser.Status.OK;

						addCmd.ExecuteNonQuery();

						// NOTE : 로그인은 별도로 해야한다.
					}

					send.SetParameter(sendParam);
				});
			});

			// 유저 탈퇴
			procedurePool.AddProcedure<EmptyParam, EmptyParam>("ReqDeleteUser", "RespDeleteUser", UserType.Registered, (recv, send) =>
			{
				var id	= authServer.GetUserIDFromAuthKey(recv.header.authKey);

				sqlHelper.RunSqlSessionWithTransaction((sql) =>
				{
					var cmd	= sql.CreateCommand();
					cmd.CommandText	= "delete from user where iduser = @id";
					cmd.Parameters.AddWithValue("@id", id);
					cmd.ExecuteNonQuery();

					return true;
				});
			});

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

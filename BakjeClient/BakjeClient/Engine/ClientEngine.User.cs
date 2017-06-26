using BakjeProtocol;
using BakjeProtocol.Parameters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BakjeClient.Engine
{
	public partial class ClientEngine
	{
		public enum NewUserResult
		{
			None,
			Success,
			DuplicatedID,
			AlreadyRegisteredEmail,
		}

		public interface IUser
		{
			NewUserResult NewUser(string userid, string password, string email);
			void DeleteUser();
			bool BlindUser(string userID, bool setBlind);
			RespUserInfo ShowUserInfo(string userID);
		}

		protected class User : AutoProcedurePool, IUser
		{
			public override string subURL => "/user/";

			public User(ClientEngine engine) : base(engine)
			{
			}

			protected override void InitParamPairs()
			{
				AddParamPair<ReqNewUser, RespNewUser>();
				AddParamPair<EmptyParam, EmptyParam>("ReqDeleteUser", "RespDeleteUser");
				AddParamPair<ReqBlindUser, EmptyParam>(recvTypeStr: "RespBlindUser");
				AddParamPair<ReqUserInfo, RespUserInfo>();
			}

			public NewUserResult NewUser(string userid, string password, string email)
			{
				var result = NewUserResult.None;

				DoRequest<ReqNewUser, RespNewUser>((sendObj) =>
				{
					sendObj.SetParameter(new ReqNewUser() { userid = userid, password = password, email = email });
				},
				(recvObj) =>
				{
					if (recvObj.header.code == Packet.Header.Code.OK)
					{
						result = NewUserResult.Success;
					}
					else if (recvObj.param.status == RespNewUser.Status.DuplicatedID)
					{
						result = NewUserResult.DuplicatedID;
					}
					else if (recvObj.param.status == RespNewUser.Status.AlreadyRegisteredEmail)
					{
						result = NewUserResult.AlreadyRegisteredEmail;
					}
				});

				return result;
			}

			public void DeleteUser()
			{
				DoRequest<EmptyParam, EmptyParam>("ReqDeleteUser", (sendObj) =>
				{

				},
				(recvObj) =>
				{
					authClient.Clear();
				});
			}

			public bool BlindUser(string userID, bool setBlind)
			{
				var result = false;

				DoRequest<ReqBlindUser, EmptyParam>(
					(send) =>
					{
						send.SetParameter(new ReqBlindUser { userID = userID, setBlind = setBlind });
					},
					(recv) =>
					{
						result = true;
					});

				return result;
			}

			public RespUserInfo ShowUserInfo(string userID)
			{
				RespUserInfo result = null;

				DoRequest<ReqUserInfo, RespUserInfo>(
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
		}
	}
}

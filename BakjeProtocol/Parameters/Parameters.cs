using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BakjeProtocol.Parameters
{
	/// <summary>
	/// 빈 파라미터
	/// </summary>
	public class EmptyParam
	{

	}

	/// <summary>
	/// 로그인 요청시 파라미터 
	/// </summary>
	public class ReqLogin
	{
		public string	userid;
		public string	password;
	}

	/// <summary>
	/// 로그인 요청 응답
	/// </summary>
	public class RespLogin
	{
		public string			authKey;
		public Auth.UserType	userType;

		/// <summary>
		/// 응답 코드
		/// </summary>
		public enum Status
		{
			OK			= 0,

			NoUserInfo,					// 유저 정보 없음 (ID없음)
			WrongPassword,				// 패스워드 불일치
			BlindedUser,				// 블라인드 처리된 유저
		}

		public Status			status;
	}
}

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

	/// <summary>
	/// 계정 생성 요청
	/// </summary>
	public class ReqNewUser
	{
		public string	userid;
		public string	password;
		public string	email;
	}

	/// <summary>
	/// 계정 생성 요청 응답
	/// </summary>
	public class RespNewUser
	{
		public enum Status
		{
			OK				= 0,
			DuplicatedID,
			AlreadyRegisteredEmail,
		}

		public Status		status;
	}
	//================================================================

	/// <summary>
	/// 포스팅 검색 요청
	/// </summary>
	public class ReqLookupPosting
	{
		public string		keyword;
		public bool			search_tag		= true;
		public bool			search_title	= true;
		public bool			search_desc		= true;
		public bool			search_user		= true;

		public int			page			= 0;
		public int			rowPerPage		= 20;
	}

	/// <summary>
	/// 포스팅 검색 응답
	/// </summary>
	public class RespLookupPosting
	{
		public class Entry
		{
			public int		postID;
			public string	author;
			public string	title;
		}

		public Entry[]		entries;

		public int			currentPage;
		public int			totalPage;
	}

	/// <summary>
	/// 포스팅 열기 요청
	/// </summary>
	public class ReqShowPosting
	{
		public int			postID;
	}

	/// <summary>
	/// 포스팅 열기 응답
	/// </summary>
	public class RespShowPosting
	{
		public string author;
		public string title;
		public string desc;

		public string[] mytags;
		public string[] othertags;

		// 이미지는 별도 필드로 전송됨
	}

	/// <summary>
	/// 포스팅 업로드 요청
	/// </summary>
	public class ReqNewPosting
	{
		public string		title;
		public string		desc;

		// NOTE : 캡쳐 업로드는 별도 데이터 필드로
		// 태그 추가는 직접 해당 페이지에 들어가서 하도록
	}

	/// <summary>
	/// 포스팅 업로드/수정에 대한 응답
	/// </summary>
	public class RespPosingModify
	{
		public enum Status
		{
			OK,
		}

		public Status		status;
	}
}

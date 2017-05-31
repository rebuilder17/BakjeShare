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

	/// <summary>
	/// 유저 블라인드(설정/해제) 요청
	/// </summary>
	public class ReqBlindUser
	{
		public string userID;
		public bool setBlind;
	}

	/// <summary>
	/// 유저 정보 요청
	/// </summary>
	public class ReqUserInfo
	{
		public string userID;
	}

	/// <summary>
	/// 유저 정보 응답
	/// </summary>
	public class RespUserInfo
	{
		public string userID;
		public string email;
		public bool isBlinded;
		public bool isAdmin;
	}

	//================================================================

	/// <summary>
	/// 포스팅 검색 요청
	/// </summary>
	public class ReqLookupPosting
	{
		public string []	keyword_title;
		public string []	keyword_desc;
		public string	 	keyword_tag;
		public string		keyword_user;
		
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

			public DateTime	postingTime;
			public bool		isPrivate;
			public bool		isBlinded;
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

		public string sourceURL;
		public DateTime datetime;
		public bool isPrivate;
		public bool isBlinded;

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
		public string		sourceURL;
		public bool			isPrivate;

		// NOTE : 캡쳐 업로드는 별도 데이터 필드로
		// 태그 추가는 직접 해당 페이지에 들어가서 하도록
	}

	/// <summary>
	/// 포스팅 업로드/수정에 대한 응답
	/// </summary>
	public class RespPostingModify
	{
		public enum Status
		{
			OK,
		}

		public Status		status;
		public int			postID;
	}

	/// <summary>
	/// 포스팅 삭제 요청
	/// </summary>
	public class ReqDeletePosting
	{
		public int			postID;
	}

	/// <summary>
	/// 포스팅 삭제 응답
	/// </summary>
	public class RespDeletePosting
	{
		public enum Status
		{
			OK,
		}

		public Status		status;
	}

	/// <summary>
	/// 태그 추가 요청
	/// </summary>
	public class ReqAddTag
	{
		public int			postID;
		public string		tagname;
	}

	/// <summary>
	/// 태그 추가 응답
	/// </summary>
	public class RespAddTag
	{
		public enum Status
		{
			OK,
			Duplicated,
		}

		public Status		status;
	}

	/// <summary>
	/// 태그 제거 요청
	/// </summary>
	public class ReqDeleteTag
	{
		public int		postID;
		public string	tagname;
	}

	/// <summary>
	/// 태그 제거 응답
	/// </summary>
	public class RespDeleteTag
	{
		public enum Status
		{
			OK,
		}

		public Status		status;
	}
	//================================================================

	/// <summary>
	/// 리포팅 요청
	/// </summary>
	public class ReqFileReport
	{
		/// <summary>
		/// 리포트 종류
		/// </summary>
		public enum Type
		{
			Posting,
			User,
			Bug,
		}

		public Type		type;
		public string	shortdesc;
		public string	longdesc;

		public int		reportingPostID;
		public string	reportingUserID;

		public enum PostReportReason
		{
			Etc,
		}
		public PostReportReason postReportReason;

		public enum UserReportReason
		{
			Etc,
		}
		public UserReportReason userReportReason;


		// Utility

		public static string ReportTypeToString(Type type)
		{
			switch(type)
			{
				case Type.Posting:
					return "posting";
				case Type.User:
					return "user";
				case Type.Bug:
					return "bug";
			}
			return null;
		}
		
		public static Type ReportTypeFromString(string typestr)
		{
			switch(typestr)
			{
				case "posting":
					return Type.Posting;
				case "user":
					return Type.User;
				case "bug":
				default:
					return Type.Bug;
			}
		}
	}

	/// <summary>
	/// 리포팅 응답
	/// </summary>
	public class RespFileReport
	{
		public enum Status
		{
			OK,
		}
		public Status		status;
	}

	/// <summary>
	/// 리포트 목록 조회 요청 (일반 리스트)
	/// </summary>
	public class ReqLookupReport
	{
		public int		page;
		public int		rowPerPage;
	}

	/// <summary>
	/// 리포트 목록 조회 응답
	/// </summary>
	public class RespLookupReport
	{
		public class Entry
		{
			public int					reportID;
			public ReqFileReport.Type	type;
			//public int					postID;
			//public string				userID;
			public string				shortdesc;
			public string				reporterID;
		}

		public Entry[]	entries;

		public int		currentPage;
		public int		totalPage;
	}

	/// <summary>
	/// 리포트 내용 요청
	/// </summary>
	public class ReqShowReport
	{
		public int reportID;
	}

	/// <summary>
	/// 리포트 내용 응답
	/// </summary>
	public class RespShowReport
	{
		public string				reporterID;
		public ReqFileReport.Type	type;
		public string				shortdesc;
		public string				longdesc;

		public int								repPostingID;
		public ReqFileReport.PostReportReason	postingRepReason;
		public string							repUserID;
		public ReqFileReport.UserReportReason	userRepReason;
	}

	/// <summary>
	/// 리포트 삭제 요청
	/// </summary>
	public class ReqCloseReport
	{
		public int			postID;
	}

	/// <summary>
	/// 리포트 삭제 응답
	/// </summary>
	public class RespCloseReport
	{
		public enum Status
		{
			OK,
		}
		public Status		status;
	}
	//================================================================

	/// <summary>
	/// 공지 올리기 요청
	/// </summary>
	public class ReqPostNotice
	{
		public string title;
		public string desc;
	}

	/// <summary>
	/// 공지 삭제 요청
	/// </summary>
	public class ReqDeleteNotice
	{
		public int noticeID;
	}

	/// <summary>
	/// 공지 목록 열람 요청
	/// </summary>
	public class ReqLookupNotice
	{
		public int page;
		public int rowperpage;
	}

	/// <summary>
	/// 공지 목록 열람 응답
	/// </summary>
	public class RespLookupNotice
	{
		public class Entry
		{
			public int noticeID;
			public string title;
			public DateTime datetime;
		}

		public Entry[] entries;

		public int currentPage;
		public int totalPage;
	}

	/// <summary>
	/// 공지 열기 요청
	/// </summary>
	public class ReqShowNotice
	{
		public int noticeID;
	}

	/// <summary>
	/// 공지 열기 응답
	/// </summary>
	public class RespShowNotice
	{
		public string title;
		public string desc;
		public DateTime datetime;
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BakjeProtocol.Auth
{
	public enum UserType
	{
		Guest,
		Registered,
		Administrator,
	}

	/// <summary>
	/// 유저에 따라 Auth Key를 발급하고 권한 레벨을 조회하는 기능을 제공한다. 내부적으로 cacheing을 한다.
	/// </summary>
	public abstract class BaseAuthServer
	{
		private class AuthInfoCache
		{
			public string		userid;
			public UserType		usertype;
			public string		authkey;
		}

		// Members

		Dictionary<string, AuthInfoCache>	m_useridToInfoCache;			// userid => auth info 캐시
		Dictionary<string, AuthInfoCache>	m_authkeyToInfoCache;			// authkey => auth info 캐시


		public BaseAuthServer()
		{
			m_useridToInfoCache		= new Dictionary<string, AuthInfoCache>();
			m_authkeyToInfoCache	= new Dictionary<string, AuthInfoCache>();
		}

		private void CacheInfo(AuthInfoCache info)
		{
			m_useridToInfoCache[info.userid]	= info;
			m_authkeyToInfoCache[info.authkey]	= info;
		}

		private AuthInfoCache ReadCacheByUserID(string userid)
		{
			AuthInfoCache cache = null;
			m_useridToInfoCache.TryGetValue(userid, out cache);
			return cache;
		}

		private AuthInfoCache ReadCacheByAuthKey(string authkey)
		{
			AuthInfoCache cache = null;
			m_authkeyToInfoCache.TryGetValue(authkey, out cache);
			return cache;
		}

		/// <summary>
		/// user에게 새 authkey를 생성해준다.
		/// </summary>
		/// <param name="userid"></param>
		/// <returns></returns>
		public string SetupNewAuthKey(string userid, UserType usertype)
		{
			var newkey	= GenerateAuthKey(userid);
			QuerySetAuthInfo(userid, newkey, usertype);

			var cache	= new AuthInfoCache()
			{
				userid		= userid,
				usertype	= usertype,
				authkey		= newkey
			};
			CacheInfo(cache);					// auth 정보를 캐싱해둔다.

			return newkey;
		}

		/// <summary>
		/// 현재 세팅된 authkey를 가져온다. auth되지 않은 경우엔 null을 리턴.
		/// </summary>
		/// <param name="userid"></param>
		/// <returns></returns>
		public string GetCurrentAuthKey(string userid)
		{
			if (userid == null)												// ID가 null일 경우 Guest임 : authkey도 null
				return null;

			var cache	= ReadCacheByUserID(userid);						// 캐시에서 값을 읽어본다.
			if (cache == null)												// 캐시에 값이 없으면 쿼리해온뒤 캐시를 채운다.
			{
				cache	= new AuthInfoCache()
				{
					userid		= userid,
					usertype	= QueryUserType(userid),
					authkey		= QueryAuthKey(userid),
				};
				CacheInfo(cache);
			}

			return cache.authkey;
		}

		/// <summary>
		/// 유저의 종류를 가져온다.
		/// </summary>
		/// <param name="userid"></param>
		/// <returns></returns>
		public UserType GetUserAuthType(string userid)
		{
			if (userid == null)												// ID 가 null이면 Guest임
				return UserType.Guest;

			var cache	= ReadCacheByUserID(userid);						// 캐시에서 값을 읽어본다.
			if (cache == null)												// 캐시에 값이 없으면 쿼리해온뒤 캐시를 채운다.
			{
				cache	= new AuthInfoCache()
				{
					userid		= userid,
					usertype	= QueryUserType(userid),
					authkey		= QueryAuthKey(userid),
				};
				CacheInfo(cache);
			}
			return cache.usertype;
		}

		/// <summary>
		/// authkey에 연결된 UserID를 가져온다.
		/// </summary>
		/// <returns></returns>
		public string GetUserIDFromAuthKey(string authkey)
		{
			if (authkey == null)											// authkey가 null이면 Guest : ID도 null임
				return null;

			var cache	= ReadCacheByAuthKey(authkey);						// 캐시에서 값을 읽어본다.
			if (cache == null)												// 캐시에 값이 없으면 쿼리해온뒤 캐시를 채운다.
			{
				var userid		= QueryUserID(authkey);
				cache	= new AuthInfoCache()
				{
					userid		= userid,
					usertype	= QueryUserType(userid),
					authkey		= authkey,
				};
				CacheInfo(cache);
			}
			return cache.userid;
		}

		/// <summary>
		/// authkey가 현재 세팅된 것과 일치하는지 본다.
		/// </summary>
		/// <param name="userid"></param>
		/// <param name="authkey"></param>
		/// <returns></returns>
		public bool CheckAuthKey(string userid, string authkey)
		{
			return GetCurrentAuthKey(userid) == authkey;
		}

		//================================================================

		/// <summary>
		/// userid를 사용하여 임의의 AuthKey를 생성한다.
		/// </summary>
		/// <returns></returns>
		protected abstract string GenerateAuthKey(string userid);

		/// <summary>
		/// 유저의 authkey를 쿼리해온다.
		/// </summary>
		/// <param name="userid"></param>
		/// <returns></returns>
		protected abstract string QueryAuthKey(string userid);

		/// <summary>
		/// 유저의 UserType를 쿼리해온다.
		/// </summary>
		/// <param name="userid"></param>
		/// <returns></returns>
		protected abstract UserType QueryUserType(string userid);

		/// <summary>
		/// authkey를 사용하여 userid를 쿼리해온다.
		/// </summary>
		/// <param name="authkey"></param>
		/// <returns></returns>
		protected abstract string QueryUserID(string authkey);

		/// <summary>
		/// 유저 authkey와 usertype를 테이블에 세팅한다.
		/// </summary>
		/// <param name="userid"></param>
		/// <param name="authkey"></param>
		/// <param name="usertype"></param>
		protected abstract void QuerySetAuthInfo(string userid, string authkey, UserType usertype);
	}
}

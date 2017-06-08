using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BakjeProtocol;
using BakjeProtocol.Auth;

namespace BakjeClient.Engine
{
	/// <summary>
	/// BakjeServer와 직접 통신하기 위한 객체
	/// </summary>
	public class ClientEngine
	{
		/// <summary>
		/// Auth키를 보관하고 관리하는 클래스
		/// </summary>
		class AuthClient : BakjeProtocol.Auth.BaseAuthClient
		{
			const string	c_keyAuthKey	= "authkey";
			const string	c_keyAuthLevel	= "authlevel";
			protected override void ClearLocalAuthToken()
			{
				var dict	= App.Current.Properties;

				if (dict.ContainsKey(c_keyAuthKey))
				{
					dict.Remove(c_keyAuthKey);
				}

				if (dict.ContainsKey(c_keyAuthLevel))
				{
					dict.Remove(c_keyAuthLevel);
				}
			}

			protected override void LoadLocalAuthToken(out string authKey, out UserType userType)
			{
				var dict		= App.Current.Properties;

				var loadedKey	= (object)null;
				dict.TryGetValue(c_keyAuthKey, out loadedKey);
				authKey			= (loadedKey ?? null) as string;

				var loadedLevel	= (object)null;
				dict.TryGetValue(c_keyAuthLevel, out loadedLevel);
				userType		= (UserType)((int)(loadedLevel ?? 0));
			}

			protected override void SaveLocalAuthToken(string newkey, UserType ut)
			{
				var dict				= App.Current.Properties;
				dict[c_keyAuthKey]		= newkey;
				dict[c_keyAuthLevel]	= (int)ut;
			}
		}
		//

		/// <summary>
		/// 현재 세팅된 서버 URL
		/// </summary>
		public string serverURL { get; private set; }


		/// <summary>
		/// url인지 ip인지 감지해서 적절하게 url 설정
		/// </summary>
		/// <param name="value"></param>
		public void SetServerURLorIP(string value)
		{
			var vlen	= value.Length;
			var isURL	= false;
			for (var i = 0; i < vlen; i++)					// value를 구성하는 문자를 보고 URL인지 판단한다.
			{
				var c	= value[i];
				if (!char.IsNumber(c) && c != '.')			// 숫자랑 . 이외의 문자가 있다면 URL로 취급한다.
				{
					isURL = true;
					break;
				}
			}

			if (isURL)
			{
				if (value[vlen - 1] == '/')					// URL의 맨 끝 '/' 문자를 제거한다. (있을 경우)
					value = value.Substring(0, vlen - 1);

				serverURL	= value;
			}
			else
			{
				serverURL	= string.Format("http://{0}:8080", value);	// IP 주소를 써서 url을 설정한다.
			}
		}
	}
}

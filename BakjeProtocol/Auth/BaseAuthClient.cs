using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BakjeProtocol.Auth
{
	public abstract class BaseAuthClient
	{
		// Members

		public string		authKey { get; private set; }
		public UserType		userType { get; private set; }


		public BaseAuthClient()
		{
			string localkey;
			UserType localut;

			LoadLocalAuthToken(out localkey, out localut);			// 초기화시에 로컬 토큰을 불러온다.
			authKey		= localkey;
			userType	= localut;
		}

		public void SetNew(string newkey, UserType usertype = UserType.Registered)
		{
			this.authKey	= newkey;
			this.userType	= usertype;

			SaveLocalAuthToken();
		}

		public void Clear()
		{
			authKey		= null;
			userType	= UserType.Guest;

			ClearLocalAuthToken();
		}

		//

		protected abstract void LoadLocalAuthToken(out string authKey, out UserType userType);
		protected abstract void SaveLocalAuthToken();
		protected abstract void ClearLocalAuthToken();
	}
}

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BakjeProtocol;
using BakjeProtocol.Auth;
using System.Net;
using System.Net.Http;

namespace BakjeClient.Engine
{
	/// <summary>
	/// BakjeServer와 직접 통신하기 위한 객체
	/// </summary>
	public partial class ClientEngine
	{
		/// <summary>
		/// Auth키를 보관하고 관리하는 클래스
		/// </summary>
		protected class AuthClient : BakjeProtocol.Auth.BaseAuthClient
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
				App.Current.SavePropertiesAsync();
			}

			protected override void LoadLocalAuthToken(out string authKey, out UserType userType)
			{
				var dict		= App.Current.Properties;

				var loadedKey	= (object)null;
				dict.TryGetValue(c_keyAuthKey, out loadedKey);
				authKey			= loadedKey == null ? null : (loadedKey as string);

				var loadedLevel	= (object)null;
				dict.TryGetValue(c_keyAuthLevel, out loadedLevel);
				userType		= loadedLevel == null? UserType.Guest : (UserType)((int)loadedLevel);
			}

			protected override void SaveLocalAuthToken(string newkey, UserType ut)
			{
				var dict				= App.Current.Properties;
				dict[c_keyAuthKey]		= newkey;
				dict[c_keyAuthLevel]	= (int)ut;
				App.Current.SavePropertiesAsync();
			}
		}

		protected abstract class AutoProcedurePool
		{
			class Bridge : BaseProcedurePool.IBridge
			{
				BaseProcedurePool.IPoolController			m_poolCtrl;
				AutoProcedurePool		m_pp;
				HttpClient				m_httpClient;
				

				public Bridge(AutoProcedurePool pool)
				{
					m_pp					= pool;
					m_httpClient			= new HttpClient(new ModernHttpClient.NativeMessageHandler());
					//m_httpClient			= new HttpClient();
					m_httpClient.Timeout	= TimeSpan.FromMilliseconds(5000);
				}

				public void SetPoolController(BaseProcedurePool.IPoolController ctrler)
				{
					m_poolCtrl		= ctrler;
				}

				public void SetupSendDelegate()
				{
					m_poolCtrl.SetSendDelegate((packet) => PacketSend(packet, m_pp.subURL, this));
				}

				void PacketSend(Packet packet, string suburl, Bridge bridge)
				{
					//var allDone				= new System.Threading.ManualResetEvent(false);

					var buffer				= packet.Pack();
					var postContent			= new ByteArrayContent(buffer);
					postContent.Headers.ContentType	= System.Net.Http.Headers.MediaTypeHeaderValue.Parse("application/octet-stream");
					
					try
					{
						var sendTask	= m_httpClient.PostAsync(m_pp.engine.serverURL + suburl, postContent);
						sendTask.Wait();

						var response	= sendTask.Result;
						var readTask	= response.Content.ReadAsByteArrayAsync();
						readTask.Wait();

						var readbuf		= readTask.Result;
						bridge.m_poolCtrl.CallReceive(Packet.Unpack(readbuf));
					}
					catch (FormatException formatEx)			// FormatException - url 잘못되었을시... 이건 외부로 토스
					{
						throw formatEx;
					}
					catch (Exception)							// 웹 연결이 에러 코드를 주면 Server Error로 처리한다.
					{
						bridge.m_poolCtrl.CallReceiveServerError(packet.header.messageType);
					}
					finally
					{
						//allDone.Set();
					}
					
					//allDone.WaitOne();
				}
			}
			//

			private ClientProcedurePool	m_procPool;

			protected ClientEngine			engine { get; private set; }
			protected AuthClient authClient
			{
				get
				{
					return engine.m_authClient;
				}
			}

			/// <summary>
			/// 프로시저 호출할 url (상속해서 지정해줘야한다)
			/// </summary>
			public abstract string subURL { get; }


			public AutoProcedurePool(ClientEngine engine)
			{
				this.engine	= engine;
				m_procPool	= new ClientProcedurePool();

				InitParamPairs();

				var bridge	= new Bridge(this);
				m_procPool.SetBridge(bridge);
				m_procPool.SetAuthClientObject(authClient);
				bridge.SetupSendDelegate();
				m_procPool.Start();
			}

			protected void AddParamPair<SendParamT, RecvParamT>(string sendTypeStr = null, string recvTypeStr = null)
				where SendParamT : class
				where RecvParamT : class
			{
				// 이름을 지정하지 않은 경우엔 타입 이름을 그대로 넣어준다.
				if (sendTypeStr == null)
					sendTypeStr	= typeof(SendParamT).Name;
				if (recvTypeStr == null)
					recvTypeStr = typeof(RecvParamT).Name;

				m_procPool.AddPairParamType<SendParamT, RecvParamT>(sendTypeStr, recvTypeStr);
			}

			protected void DoRequest<SendParamT, RecvParamT>(string sendTypeStr, Action<BaseProcedurePool.ISend<SendParamT>> sendFunc, Action<BaseProcedurePool.IReceive<RecvParamT>> recvFunc)
				where SendParamT : class
				where RecvParamT : class
			{
				m_procPool.DoRequest<SendParamT, RecvParamT>(sendTypeStr, sendFunc, recvFunc);
			}

			protected void DoRequest<SendParamT, RecvParamT>(Action<BaseProcedurePool.ISend<SendParamT>> sendFunc, Action<BaseProcedurePool.IReceive<RecvParamT>> recvFunc)
				where SendParamT : class
				where RecvParamT : class
			{
				DoRequest<SendParamT, RecvParamT>(typeof(SendParamT).Name, sendFunc, recvFunc);
			}

			/// <summary>
			/// Parameter pair 설정해주는 시점
			/// </summary>
			protected abstract void InitParamPairs();
		}
		//

		/// <summary>
		/// 현재 세팅된 서버 URL
		/// </summary>
		public string serverURL { get; private set; }

		private AuthClient m_authClient;					// Auth 관리자
		
		/// <summary>
		/// 현재 획득한 유저 권한 (로컬)
		/// </summary>
		public BakjeProtocol.Auth.UserType authLevel
		{
			get { return m_authClient.userType; }
		}

		public IAuth auth { get; private set; }
		public IUser user { get; private set; }
		public IPost post { get; private set; }
		public INotice notice { get; private set; }
		public IReport report { get; private set; }


		public ClientEngine()
		{
			
		}
		
		public void Initialize()
		{
			m_authClient	= new AuthClient();
			auth			= new Auth(this);
			user			= new User(this);
			post			= new Post(this);
			notice			= new Notice(this);
			report			= new Report(this);
		}

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
				serverURL	= string.Format("http://{0}:8084", value);	// IP 주소를 써서 url을 설정한다.
			}
		}
	}
}

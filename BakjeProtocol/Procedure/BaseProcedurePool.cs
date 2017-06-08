using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BakjeProtocol
{
	/// <summary>
	/// 프로토콜을 사용해서 전송받거나 혹은 전송할 메세지를 처리할 프로시저들을 관리
	/// </summary>
	public abstract class BaseProcedurePool
	{
		/// <summary>
		/// ProcedurePool과 외부 다른 객체와의 통신을 위한 인터페이스.
		/// </summary>
		public interface IBridge
		{
			void SetPoolController(IPoolController ctrler);
		}

		/// <summary>
		/// IBridge를 구현한 객체에서 Pool의 내부 기능 몇가지를 쓸 수 있게 해주는 인터페이스
		/// </summary>
		public interface IPoolController
		{
			/// <summary>
			/// ProcedurePool 이 해당 패킷을 받도록 한다.
			/// </summary>
			/// <param name="packet"></param>
			void CallReceive(Packet packet, Action<Packet> responseDel = null);

			/// <summary>
			/// ProcedurePool 이 recv 과정에서 발생한 에러 처리를 하도록 한다.
			/// 주의 : Client side에서 호출하도록 설계되었다. (Send 후에 Recv를 받는 상황)
			/// </summary>
			void CallReceiveServerError(string sendTypeStr);

			/// <summary>
			/// ProcedurePool 에서 보내려는 패킷을 처리할 델리게이트를 지정
			/// </summary>
			/// <param name="sendDel"></param>
			void SetSendDelegate(Action<Packet> sendDel);
		}

		/// <summary>
		/// 전송받은 내용
		/// </summary>
		public interface IReceive<T> : IReceive
		{
			T				param { get; }
		}

		/// <summary>
		/// 전송받은 내용
		/// </summary>
		public interface IReceive
		{
			Packet.Header	header { get; }
			int				binaryDataCount { get; }
			Action<Packet>	responseCallback { get; set; }

			byte[] GetBinaryData(int index);
		}

		/// <summary>
		/// 전송해야할 내용
		/// </summary>
		public interface ISend<T> : ISend
		{
			void SetParameter(T param);
		}

		/// <summary>
		/// 전송해야할 내용
		/// </summary>
		public interface ISend
		{
			Packet.Header	header { get; }
			//Action<Packet>	responseCallback { get; set; }

			void AddBinaryData(byte[] data);
		}
		//

		private class PoolController : IPoolController
		{
			BaseProcedurePool m_parent;
			Action<Packet>	m_sendDel;

			public PoolController(BaseProcedurePool parent)
			{
				m_parent	= parent;
			}

			public void CallReceive(Packet packet, Action<Packet> responseDel = null)
			{
				m_parent.ProcessReceivePacket(packet, responseDel);
			}

			public void SetSendDelegate(Action<Packet> sendDel)
			{
				m_sendDel	= sendDel;
			}

			public void CallSend(Packet packet)
			{
				m_sendDel(packet);
			}

			public void CallReceiveServerError(string sendTypeStr)
			{
				m_parent.ProcessReceiveServerError(sendTypeStr);
			}
		}

		private class ReceiveGeneral : IReceive
		{
			Packet		m_packet;

			public Action<Packet>	responseCallback { get; set; }

			public Packet.Header header
			{
				get { return m_packet.header; }
			}

			public int binaryDataCount
			{
				get { return m_packet.binaryDataCount; }
			}

			public byte[] GetBinaryData(int index)
			{
				return m_packet.GetBinaryData(index);
			}

			public ReceiveGeneral(Packet packet)
			{
				m_packet	= packet;
			}
		}

		private class Receive<T> : ReceiveGeneral, IReceive<T>
			where T : class
		{
			T			m_param;

			public T param
			{
				get { return m_param; }
			}

			public Receive(Packet packet) : base(packet)
			{
				m_param		= packet.GetJSONData<T>();
			}
		}

		private abstract class SendGeneral : ISend
		{
			protected Packet		m_packet;

			//public Action<Packet>	responseCallback { get; set; }

			public Packet.Header header
			{
				get { return m_packet.header; }
			}

			public void AddBinaryData(byte[] data)
			{
				m_packet.AddBinaryData(data);
			}

			public Packet packet
			{
				get { return m_packet; }
			}

			public SendGeneral()
			{
				m_packet	= new Packet();
			}
		}

		private class Send<T> : SendGeneral, ISend<T>
			where T : class
		{
			public void SetParameter(T param)
			{
				m_packet.SetJSON(param);
			}
		}

		/// <summary>
		/// 메세지 종류에 따른 타입 정보
		/// </summary>
		private class MessageTypeInfo
		{
			public string typeStr;		// 패킷 헤더에 포함되는 메세지 문자열
			public Type paramType;		// 매칭되는 패킷 파라미터 타입

			public Func<Packet, ReceiveGeneral> funcMakeRecv;	// IReceive<T> 오브젝트를 만드는 함수
			public Func<SendGeneral> funcMakeSend;				// ISend<T> 오브젝트를 만드는 함수
		}


		// Members

		Dictionary<string, MessageTypeInfo>	m_typeInfoDict;				// 메세지 타입 정보 딕셔너리
		Dictionary<string, string>			m_typePairDict;				// 타입 페어 정보 딕셔너리
		Dictionary<string, Action<object>>	m_recvCallbackDict;			// 패킷 받았을 때 호출할 함수 딕셔너리
		//Dictionary<string, Action<object>>	m_sendFunctionDict;			// 패킷 보낼 때 호출할 함수 딕셔너리

		PoolController			m_poolCtrl;

		/// <summary>
		/// Start를 했는지 여부. start하지 않은 상태에서는 패킷 처리 요청 등을 전부 무시한다. start한 뒤에는 함수 추가 등등이 안된다. (Exception발생)
		/// Dictionary가 read에 대해서는 thread-safe를 보장하지만 write에 대해서는 그렇지 않으므로 이러한 제한을 걸어두었다.
		/// </summary>
		public bool started { get; private set; }


		public BaseProcedurePool()
		{
			m_poolCtrl	= new PoolController(this);

			m_typeInfoDict	= new Dictionary<string, MessageTypeInfo>();
			m_typePairDict	= new Dictionary<string, string>();
			m_recvCallbackDict	= new Dictionary<string, Action<object>>();
			//m_sendFunctionDict	= new Dictionary<string, Action<object>>();
		}

		private void ThrowExceptionIfAlreadyStarted()
		{
			if (started) throw new InvalidOperationException("cannot do the operation when the pool is started.");
		}

		/// <summary>
		/// 다른 객체와 브릿지 연결
		/// </summary>
		/// <param name="bridge"></param>
		public void SetBridge(IBridge bridge)
		{
			ThrowExceptionIfAlreadyStarted();	// 시작하면 실행할 수 없음

			bridge.SetPoolController(m_poolCtrl);
		}

		/// <summary>
		/// 처리를 시작한다. 시작하면 프로시저를 새로 등록할 수 없다.
		/// </summary>
		public void Start()
		{
			started	= true;
		}

		/// <summary>
		/// 처리를 멈춘다.
		/// </summary>
		public void Stop()
		{
			started	= false;
		}

		protected bool MessageTypeRegistered(string typeStr)
		{
			return m_typeInfoDict.ContainsKey(typeStr);
		}

		/// <summary>
		/// 패킷 메세지 타입 문자열과 실제 파라미터 타입 매칭을 추가한다.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="typeStr"></param>
		protected void AddMessageType<T>(string typeStr)
			where T : class
		{
			ThrowExceptionIfAlreadyStarted();	// 시작하면 실행할 수 없음

			var typeInfo	= new MessageTypeInfo()
			{
				typeStr		= typeStr,
				paramType	= typeof(T)
			};

			typeInfo.funcMakeRecv	= (packet) =>
			{
				var obj	= new Receive<T>(packet);
				return obj;
			};

			typeInfo.funcMakeSend	= () =>
			{
				var obj	= new Send<T>();
				return obj;
			};

			m_typeInfoDict[typeStr]	= typeInfo;
		}

		/// <summary>
		/// 패킷 메세지 타입 문자열을 매칭해준다.
		/// </summary>
		/// <param name="typeStr1"></param>
		/// <param name="typeStr2"></param>
		protected void AddMessageTypePair(string typeStr1, string typeStr2)
		{
			ThrowExceptionIfAlreadyStarted();	// 시작하면 실행할 수 없음

			m_typePairDict[typeStr1] = typeStr2;
		}

		/// <summary>
		/// 등록된 메세지 타입 페어를 구한다.
		/// </summary>
		/// <param name="typeStr"></param>
		/// <returns></returns>
		protected string LookupMessageTypePair(string typeStr)
		{
			string retv = null;
			m_typePairDict.TryGetValue(typeStr, out retv);
			return retv;
		}

		/// <summary>
		/// 타입 매칭 정보를 가져온다.
		/// </summary>
		/// <param name="typeStr"></param>
		/// <returns></returns>
		private MessageTypeInfo LookupMessageType(string typeStr)
		{
			return m_typeInfoDict[typeStr];
		}

		/// <summary>
		/// 패킷 받을 때 호출할 콜백 함수를 등록
		/// </summary>
		/// <param name="typeStr"></param>
		/// <param name="del"></param>
		protected void AddRecvCallback(string typeStr, Action<object> del)
		{
			ThrowExceptionIfAlreadyStarted();	// 시작하면 실행할 수 없음

			m_recvCallbackDict.Add(typeStr, del);
		}

		/// <summary>
		/// 패킷 받을 때 호출할 콜백 함수를 등록.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="typeStr"></param>
		/// <param name="del"></param>
		protected void AddRecvCallback<T>(string typeStr, Action<IReceive<T>> del)
			where T : class
		{
			AddRecvCallback(typeStr, (recvobj) =>
			{
				del(recvobj as IReceive<T>);
			});
		}

		/// <summary>
		/// 패킷 받는 함수 찾기
		/// </summary>
		/// <param name="typeStr"></param>
		/// <returns></returns>
		protected virtual Action<object> LookupRecvCallback(string typeStr)
		{
			return m_recvCallbackDict[typeStr];
		}

		//protected void AddSendFunction(string typeStr, Action<object> del)
		//{
		//	ThrowExceptionIfAlreadyStarted();	// 시작하면 실행할 수 없음

		//	m_sendFunctionDict.Add(typeStr, del);
		//}

		//protected void AddSendFunction<T>(string typeStr, Action<ISend<T>> del)
		//	where T : class
		//{
		//	AddSendFunction(typeStr, (sendobj) =>
		//	{
		//		del(sendobj as ISend<T>);
		//	});
		//}

		//protected virtual Action<object> LookupSendCallback(string typeStr)
		//{
		//	return m_sendFunctionDict[typeStr];
		//}


		/// <summary>
		/// PoolController에서 Receive 들어올 때 호출한다.
		/// </summary>
		/// <param name="packet"></param>
		private void ProcessReceivePacket(Packet packet, Action<Packet> responseDel = null)
		{
			if (!started)					// 시작한 경우에만 실행한다.
				return;
			
			var messageType	= packet.header.messageType;
			var typeInfo	= LookupMessageType(messageType);		// 타입 정보 가져오기
			var recvObj		= typeInfo.funcMakeRecv(packet);		// 타입에 맞게 IReceive오브젝트 생성
			recvObj.responseCallback = responseDel;					// responseDel을 지정한 경우 세팅

			var recvfunc	= LookupRecvCallback(recvObj.header.messageType);	// recv 처리 함수 찾기
			recvfunc(recvObj);
		}

		/// <summary>
		/// PoolController에서 receive과정 중 에러 처리가 필요해질 때 호출한다. header만 적절히 세팅한 더미 패킷을 만들어서 기존에 세팅한 recv 함수로 토스한다.
		/// </summary>
		private void ProcessReceiveServerError(string sendTypeStr)
		{
			if (!started)					// 시작한 경우에만 실행한다.
				return;

			var expectedRecvTypeStr	= LookupMessageTypePair(sendTypeStr);
			var typeInfo			= LookupMessageType(expectedRecvTypeStr);

			var packet				= new Packet();								// dummy 패킷을 만들어준다.
			packet.header.code		= Packet.Header.Code.ServerSideError;		// Server side error가 벌어졌음
			var recvObj				= typeInfo.funcMakeRecv(packet);			// 적절한 recvobj로 만들어준다.

			var recvfunc	= LookupRecvCallback(expectedRecvTypeStr);			// recv 처리 함수 찾기
			recvfunc(recvObj);
		}

		/// <summary>
		/// 새 패킷 정보를 만들어 지정한 함수 델리게이트를 통해 처리한 뒤 전송한다.
		/// </summary>
		/// <param name="typeStr"></param>
		/// <param name="procfunc"></param>
		//protected void DoProcessSendPacket(string typeStr, Action<object> procfunc, Action<Packet> sendDel = null, Action<Packet> responseDel = null)
		protected void DoProcessSendPacket(string typeStr, Action<object> procfunc, Action<Packet> sendDel = null)
		{
			if (!started)					// 시작한 경우에만 실행한다.
				return;
			
			var typeInfo	= LookupMessageType(typeStr);			// 타입 정보 가져오기
			var sendObj		= typeInfo.funcMakeSend();				// 타입에 맞는 ISend 오브젝트 생성
			//sendObj.responseCallback	= responseDel;				// 응답 받을 델리게이트를 지정했다면 세팅
			sendObj.header.messageType	= typeStr;					// 메세지 타입 헤더에 넣기

			procfunc(sendObj);										// 패킷 처리 함수 실행 (패킷 내용을 채운다)

			var packet	= sendObj.packet;

			if (sendDel != null)									// 미리 지정한 전송 델리게이트가 있으면 그것을 사용
			{
				sendDel(packet);
			}
			else
			{
				m_poolCtrl.CallSend(packet);						// 외부에서 지정한 콜백으로 패킷을 넘긴다
			}
		}
	}
}

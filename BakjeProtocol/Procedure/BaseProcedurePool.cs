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
			void CallReceive(Packet packet);

			/// <summary>
			/// ProcedurePool 에서 보내려는 패킷을 처리할 델리게이트를 지정
			/// </summary>
			/// <param name="sendDel"></param>
			void SetSendDelegate(Action<Packet> sendDel);
		}

		/// <summary>
		/// 전송받은 내용
		/// </summary>
		public interface IReceive<T>
		{
			Packet.Header	header { get; }
			T				param { get; }
			int binaryDataCount { get; }
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

			public void CallReceive(Packet packet)
			{
				m_parent.ProcessReceivePacket(packet);
			}

			public void SetSendDelegate(Action<Packet> sendDel)
			{
				m_sendDel	= sendDel;
			}

			public void CallSend(Packet packet)
			{
				m_sendDel(packet);
			}
		}

		private class Receive<T> : IReceive<T>
			where T : class
		{
			Packet		m_packet;
			T			m_param;

			public Packet.Header header
			{
				get { return m_packet.header; }
			}

			public T param
			{
				get { return m_param; }
			}

			public int binaryDataCount
			{
				get { return m_packet.binaryDataCount; }
			}

			public byte[] GetBinaryData(int index)
			{
				return m_packet.GetBinaryData(index);
			}

			public Receive(Packet packet)
			{
				m_packet	= packet;
				m_param		= packet.GetJSONData<T>();
			}
		}

		private abstract class SendGeneral : ISend
		{
			protected Packet		m_packet;

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

			public Func<Packet, object> funcMakeRecv;	// IReceive<T> 오브젝트를 만드는 함수
			public Func<object> funcMakeSend;	// ISend<T> 오브젝트를 만드는 함수
		}


		// Members

		Dictionary<string, MessageTypeInfo>	m_typeInfoDict;				// 메세지 타입 정보 딕셔너리
		Dictionary<string, Action<object>>	m_recvCallbackDict;			// 패킷 받았을 때 호출할 함수 딕셔너리
		Dictionary<string, Action<object>>	m_sendFunctionDict;			// 패킷 보낼 때 호출할 함수 딕셔너리

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
			m_recvCallbackDict	= new Dictionary<string, Action<object>>();
			m_sendFunctionDict	= new Dictionary<string, Action<object>>();
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

		protected void AddSendFunction(string typeStr, Action<object> del)
		{
			ThrowExceptionIfAlreadyStarted();	// 시작하면 실행할 수 없음

			m_sendFunctionDict.Add(typeStr, del);
		}

		protected void AddSendFunction<T>(string typeStr, Action<ISend<T>> del)
			where T : class
		{
			AddSendFunction(typeStr, (sendobj) =>
			{
				del(sendobj as ISend<T>);
			});
		}

		protected virtual Action<object> LookupSendCallback(string typeStr)
		{
			return m_sendFunctionDict[typeStr];
		}


		/// <summary>
		/// PoolController에서 Receive 들어올 때 호출한다.
		/// </summary>
		/// <param name="packet"></param>
		private void ProcessReceivePacket(Packet packet)
		{
			if (!started)					// 시작한 경우에만 실행한다.
				return;
			
			var messageType	= packet.header.messageType;
			var typeInfo	= LookupMessageType(messageType);		// 타입 정보 가져오기
			var recvObj		= typeInfo.funcMakeRecv(packet);		// 타입에 맞게 IReceive오브젝트 생성

			var recvfunc	= LookupRecvCallback(messageType);		// recv 처리 함수 찾기
			recvfunc(recvObj);
		}

		/// <summary>
		/// 새 패킷 정보를 만들어 지정한 함수 델리게이트를 통해 처리한 뒤 전송한다.
		/// </summary>
		/// <param name="typeStr"></param>
		/// <param name="procfunc"></param>
		protected void DoProcessSendPacket(string typeStr, Action<object> procfunc)
		{
			if (!started)					// 시작한 경우에만 실행한다.
				return;
			
			var typeInfo	= LookupMessageType(typeStr);			// 타입 정보 가져오기
			var sendObj		= typeInfo.funcMakeSend();				// 타입에 맞는 ISend 오브젝트 생성

			procfunc(sendObj);										// 패킷 처리 함수 실행 (패킷 내용을 채운다)

			var packet	= (sendObj as SendGeneral).packet;
			m_poolCtrl.CallSend(packet);							// 외부에서 지정한 콜백으로 패킷을 넘긴다
		}
	}
}

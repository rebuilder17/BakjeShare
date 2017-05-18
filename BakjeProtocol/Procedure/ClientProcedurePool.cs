﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BakjeProtocol.Procedure
{
	public class ClientProcedurePool : BaseProcedurePool
	{
		Dictionary<string, string>	m_sendRecvMsgTypePair;	// 전송시 메세지 타입 - 받을 때 메세지 타입 매칭

		string			m_expectedRecvMsgType;				// 받아야할 recv 메세지 타입, 이것과 어긋나면 뭔가 잘못된 것
		Action<object>	m_recvCallback;						// 다음에 1회 호출할 콜백


		/// <summary>
		/// 전송하고 받을 메세지/파라미터 타입을 추가한다.
		/// </summary>
		/// <typeparam name="ParamT"></typeparam>
		/// <param name="typeStr"></param>
		public void AddPairParamType<SendParamT, ReceiveParamT>(string sendTypeStr, string recvTypeStr)
			where SendParamT : class
			where ReceiveParamT : class
		{
			AddMessageType<SendParamT>(sendTypeStr);
			if (!MessageTypeRegistered(recvTypeStr))			// 받을 때 메세지 타입도 등록한다. 단 이미 뭔가 등록된 경우엔 패스.
			{
				AddMessageType<ReceiveParamT>(recvTypeStr);
			}

			m_sendRecvMsgTypePair.Add(sendTypeStr, recvTypeStr);
		}

		protected override Action<object> LookupRecvCallback(string typeStr)
		{
			//return base.LookupRecvCallback(typeStr);
			
			if (m_expectedRecvMsgType != typeStr)			// 기대하던 콜백 타입이 안들어왔으면 exception
			{
				m_expectedRecvMsgType	= null;
				m_recvCallback			= null;

				throw new Exception(string.Format("expeced messageType is {0}, but received {1}", m_expectedRecvMsgType, typeStr));
			}

			var cb					= m_recvCallback;		// 보관하고 있던 콜백 타입을 리턴하고, 레퍼런스는 해제
			m_recvCallback			= null;
			m_expectedRecvMsgType	= null;

			return cb;
		}

		/// <summary>
		/// 메세지를 전송하고, 지정한 콜백으로 받는다.
		/// </summary>
		/// <typeparam name="SendParamT"></typeparam>
		/// <typeparam name="RecvParamT"></typeparam>
		/// <param name="sendTypeStr"></param>
		/// <param name="sendProc"></param>
		/// <param name="recvProc"></param>
		public void DoRequest<SendParamT, RecvParamT>(string sendTypeStr, Action<ISend<SendParamT>> sendProc, Action<IReceive<RecvParamT>> recvProc)
		{
			m_expectedRecvMsgType	= m_sendRecvMsgTypePair[sendTypeStr];							// 응답 돌아올 때의 메세지 타입을 지정
			m_recvCallback			= (recvobj) => recvProc(recvobj as IReceive<RecvParamT>);		// 응답 돌아올 때의 콜백 설정

			DoProcessSendPacket(sendTypeStr, (sendobj) => sendProc(sendobj as ISend<SendParamT>));	// Send 처리하기
		}
	}
}

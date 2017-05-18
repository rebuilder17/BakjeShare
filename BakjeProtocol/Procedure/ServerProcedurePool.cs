﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BakjeProtocol.Procedure
{
	/// <summary>
	/// 서버측에서 사용하는 ProcedurePool 구현
	/// </summary>
	public class ServerProcedurePool : BaseProcedurePool
	{
		/// <summary>
		/// 프로시저 추가
		/// </summary>
		/// <param name="typeStr"></param>
		/// <param name="procedue"></param>
		public void AddProcedure<RecvParamT, SendParamT>(string recvTypeStr, string sendTypeStr, Action<IReceive<RecvParamT>, ISend<SendParamT>> procedure)
			where RecvParamT : class
			where SendParamT : class
		{
			AddMessageType<RecvParamT>(recvTypeStr);					// 요청받을 때 타입을 추가한다.
			if (!MessageTypeRegistered(sendTypeStr))					// 요청에 대한 응답 전달할 때의 타입 추가, 단 중복해서 추가하지는 않도록
			{
				AddMessageType<SendParamT>(sendTypeStr);
			}
			
			AddRecvCallback<RecvParamT>(recvTypeStr, (recvobj) =>		// 패킷 받을 때의 함수를 추가한다.
			{
				DoProcessSendPacket(sendTypeStr, (sendobj) =>			// 지정한 익명 함수를 실행한 뒤 바로 전송을 개시한다.
				{
					procedure(recvobj, sendobj as ISend<SendParamT>);	// 파라미터로 지정한 프로시저를 실행한 뒤 바로 전송하게 한다...
				});
			});
		}
	}
}
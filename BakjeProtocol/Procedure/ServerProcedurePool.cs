using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BakjeProtocol
{
	/// <summary>
	/// 서버측에서 사용하는 ProcedurePool 구현
	/// </summary>
	public class ServerProcedurePool : BaseProcedurePool
	{
		// Members

		Auth.BaseAuthServer	m_authServer;

		
		public void SetAuthServerObj(Auth.BaseAuthServer authserver)
		{
			m_authServer	= authserver;
		}
		
		/// <summary>
		/// 프로시저 추가
		/// </summary>
		/// <param name="typeStr"></param>
		/// <param name="procedue"></param>
		public void AddProcedure<RecvParamT, SendParamT>(string recvTypeStr, string sendTypeStr, Auth.UserType authLevel,
															Action<IReceive<RecvParamT>, ISend<SendParamT>> procedure)
			where RecvParamT : class
			where SendParamT : class
		{
			AddMessageType<RecvParamT>(recvTypeStr);					// 요청받을 때 타입을 추가한다.
			if (!MessageTypeRegistered(sendTypeStr))					// 요청에 대한 응답 전달할 때의 타입 추가, 단 중복해서 추가하지는 않도록
			{
				AddMessageType<SendParamT>(sendTypeStr);
			}
			AddMessageTypePair(recvTypeStr, sendTypeStr);				// 메세지 타입 연관관계 등록
			
			AddRecvCallback<RecvParamT>(recvTypeStr, (recvobj) =>		// 패킷 받을 때의 함수를 추가한다.
			{
				DoProcessSendPacket(sendTypeStr, (sendobj) =>			// 지정한 익명 함수를 실행한 뒤 바로 전송을 개시한다.
				{
					var userid	= m_authServer.GetUserIDFromAuthKey(recvobj.header.authKey);	// authkey로 유저 id를 조회해본다.
					if (m_authServer.GetUserAuthType(userid) < authLevel)						// 해당 유저의 권한 레벨이 모자라면, 에러 코드를 부여해서 보내기
					{
						(sendobj as ISend<SendParamT>).header.code	= Packet.Header.Code.AuthNeeded;
					}
					else
					{
						procedure(recvobj, sendobj as ISend<SendParamT>);	// 파라미터로 지정한 프로시저를 실행한 뒤 바로 전송하게 한다...
					}
				},
				recvobj.responseCallback								// 따로 설정한 response 델리게이트가 있으면 지정한다.
				);
			});
		}
	}
}

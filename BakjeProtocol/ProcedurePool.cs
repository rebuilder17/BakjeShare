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
	public class ProcedurePool
	{
		/// <summary>
		/// ProcedurePool과 외부 다른 객체와의 통신을 위한 인터페이스. 실제 패킷 send/receive를 정의한다.
		/// </summary>
		public interface IBridge
		{

		}

		/// <summary>
		/// IBridge를 구현한 객체에서 Pool의 내부 기능 몇가지를 쓸 수 있게 해주는 인터페이스
		/// </summary>
		public interface IPoolController
		{

		}

		/// <summary>
		/// 전송받은 내용
		/// </summary>
		public interface IReceive
		{

		}

		/// <summary>
		/// 전송해야할 내용
		/// </summary>
		public interface ISend
		{

		}


		// Members


	}
}

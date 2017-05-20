using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Threading;
using System.Diagnostics;
using System.Web;
using BakjeProtocol;

namespace BakjeShareServer.Http
{
	public class Server
	{
		/// <summary>
		/// ProcedurePool에 연결하기 위한 오브젝트
		/// </summary>
		class ProcedurePoolBridge : BaseProcedurePool.IBridge
		{
			// Members

			Server								m_parent;
			BaseProcedurePool.IPoolController	m_poolCtrl;


			public ProcedurePoolBridge(Server parent)
			{
				m_parent	= parent;
			}

			public void SetPoolController(BaseProcedurePool.IPoolController ctrler)
			{
				m_poolCtrl	= ctrler;
			}

			public void Receive(Packet packet, Action<Packet> responseDel)
			{
				m_poolCtrl.CallReceive(packet, responseDel);
			}

			//public void SetSendDel(Action<Packet> del)
			//{
			//	m_poolCtrl.SetSendDelegate(del);
			//}
		}



		// Constants

		const int		c_workerThreadCount	= 16;


		// Members

		HttpListener	m_listener;

		Thread			m_threadListener;					// http 연결을 처리하는 쓰레드
		Thread []		m_threadWorkers;					// 각 http연결마다 응답을 만들어내는 쓰레드

		ManualResetEvent	m_reStop;						// 쓰레드 리셋 이벤트 - 서버 정지시
		ManualResetEvent	m_reReady;						// 쓰레드 리셋 이벤트 - worker 쓰레드 준비

		Queue<HttpListenerContext>	m_contextQueue;			// 요청의 context 큐

		Dictionary<string, Action<HttpListenerContext>>		m_contextProcessDict;	// 각 하위 주소에 맵핑되는 프로세스 처리 델리게이트 딕셔너리


		public Server()
		{
			m_reStop		= new ManualResetEvent(false);
			m_reReady		= new ManualResetEvent(false);

			m_threadWorkers	= new Thread[c_workerThreadCount];

			m_contextQueue	= new Queue<HttpListenerContext>();
			m_listener		= new HttpListener();
			m_threadListener	= new Thread(DoHandleRequest);

			m_contextProcessDict	= new Dictionary<string, Action<HttpListenerContext>>();
		}
		
		/// <summary>
		/// 서버를 가동한다.
		/// </summary>
		public void Start()
		{
			var listeningURL	= "http://+:8080/";
			m_listener.Prefixes.Add(listeningURL);
			try
			{
				m_listener.Start();							// Http 리스너 시작
			}
			catch(HttpListenerException e)					// 특정 URL에 대해서 권한 획득을 하지 못한 경우 시작하지 못하는 수도 있다.
			{
				// 특정 URL에 대한 권한 획득을 한다.

				var process				= new Process();
				var startInfo			= new ProcessStartInfo();
				startInfo.WindowStyle	= ProcessWindowStyle.Hidden;
				startInfo.FileName		= "cmd.exe";
				startInfo.Arguments		= string.Format("/C netsh http add urlacl url={0} user=Everyone listen=yes", listeningURL);
				startInfo.Verb			= "runas";
				process.StartInfo		= startInfo;
				process.Start();
				process.WaitForExit();

				m_listener				= new HttpListener();
				m_listener.Prefixes.Add(listeningURL);
				m_listener.Start();							// 다시 서버를 시작한다.
			}
			
			m_threadListener.Start();						// Http 요청 처리 쓰레드 시작

			for (var i = 0; i < c_workerThreadCount; i++)	// Worker 쓰레드 생성, 시작
			{
				var worker	= new Thread(DoWorker);
				worker.Start();

				m_threadWorkers[i]	= worker;
			}
		}

		/// <summary>
		/// 서버를 중지한다.
		/// </summary>
		public void Stop()
		{
			m_reStop.Set();									// 정지 시그널을 올린다.
			m_threadListener.Join();						// 요청 처리 쓰레드가 끝날 때까지 기다린다

			foreach (var worker in m_threadWorkers)			// Worker thread들이 하나씩 끝날 때까지 기다린다.
			{
				worker.Join();
			}

			m_listener.Stop();								// 리스너 정지
		}

		/// <summary>
		/// 들어오는 요청을 받아서 Worker thread에 처리를 맡긴다
		/// </summary>
		void DoHandleRequest()
		{
			while (m_listener.IsListening)
			{
				var context	= m_listener.BeginGetContext(OnContextReceived, m_listener);

				// Context를 받을 때까지, 혹은 정지 시그널이 올라갈 때까지 기다린다.
				if (WaitHandle.WaitAny(new [] { m_reStop, context.AsyncWaitHandle }) == 0)	// 만약 정지 시그널 때문에 Wait이 풀렸다면, 리턴
				{
					return;
				}
			}
		}
		
		void OnContextReceived(IAsyncResult ar)
		{
			lock(m_contextQueue)
			{
				m_contextQueue.Enqueue(m_listener.EndGetContext(ar));		// 대기큐에 context를 집어넣는다.
				m_reReady.Set();											// 준비 시그널을 올린다. (worker 쓰레드 가동)
			}
		}

		void DoWorker()
		{
			var waitHandles		= new [] { m_reReady, m_reStop };
			while(WaitHandle.WaitAny(waitHandles) == 0)	// 준비 혹은 정지 시그널을 기다리되 준비 시그널일 때만 루프를 지속한다.
			{
				lock(m_contextQueue)
				{
					HttpListenerContext	context	= null;

					if (m_contextQueue.Count > 0)		// 큐에 뭔가 내용이 있으면 하나 가져온다.
					{
						context	= m_contextQueue.Dequeue();
					}
					else
					{									// 큐에 내용이 없으면 준비 시그널을 해제한다. (worker쓰레드 전부 대기상태로)
						m_reReady.Reset();
					}

					if (context != null)				// 요청 처리하기
					{
						try
						{
							var suburl	= string.Join("", context.Request.Url.Segments);
							if (suburl[suburl.Length - 1] != '/')
								suburl += '/';

							Action<HttpListenerContext>	del;
							if (m_contextProcessDict.TryGetValue(suburl, out del))
							{
								del(context);
							}
							else
							{								// 등록되지 않은 경우, 404 에러
								SendHttpStatusPage(context.Response, HttpStatusCode.NotFound);
							}
						}
						catch (Exception e)				// 에러가 발생하면 500 에러
						{
							SendHttpStatusPage(context.Response, HttpStatusCode.InternalServerError);
							Console.Error.WriteLine(e.ToString());
						}
					}
				}
			}
		}

		/// <summary>
		/// 하위 URL에 컨텍스트 처리 함수를 맵핑한다. 각 함수는 별도 쓰레드에서 작동해도 thread-safe를 보장해야 한다.
		/// </summary>
		/// <param name="suburl">하위 url. /.../ 형식이어야한다. ( / 으로 둘러싸여야함)</param>
		/// <param name="del"></param>
		public void RegisterContextProcessor(string suburl, Action<HttpListenerContext> del)
		{
			if (suburl[0] != '/' || suburl[suburl.Length - 1] != '/')
			{
				throw new ArgumentException("suburl must be embraced with '/'");
			}

			m_contextProcessDict[suburl]	= del;
		}

		static void SendHttpStatusPage(HttpListenerResponse response, HttpStatusCode code)
		{
			response.StatusCode	= (int)code;
			var message			= HttpWorkerRequest.GetStatusDescription(response.StatusCode);
			response.StatusDescription	= message;

			var page			= string.Format("<!DOCTYPE html><html><head><title>{0}</title></head><body><h1>{1} - {0}</h1></body></html>", message, response.StatusCode);
			var buffer			= Encoding.UTF8.GetBytes(page);

			response.ContentEncoding	= Encoding.UTF8;
			response.ContentLength64	= buffer.Length;
			response.ContentType		= "text/html";
			response.OutputStream.Write(buffer, 0, buffer.Length);
			response.Close();
		}

		/// <summary>
		/// 특정 suburl으로 오는 요청을 ProcedurePool로 보내기 위한 Bridge를 생성한다.
		/// </summary>
		/// <param name="suburl"></param>
		/// <returns></returns>
		public BaseProcedurePool.IBridge CreateProcedurePoolBridge(string suburl)
		{
			var bridge	= new ProcedurePoolBridge(this);
			RegisterContextProcessor(suburl, (context) =>			// 해당 브릿지로 receive를 보내도록 설정
			{
				// 요청 데이터를 읽는다. (application/octet-stream)
				var req		= context.Request;
				if (req.ContentType != "application/octet-stream")
					throw new Exception(string.Format("content type is not octet-stream : {0}", req.ContentType));
				var length	= req.ContentLength64;
				var buffer	= new byte[length];
				req.InputStream.Read(buffer, 0, (int)length);

				var packet	= Packet.Unpack(buffer);					// Packet으로 만들기

				bridge.Receive(packet, (responsePacket) =>				// 브릿지 통해서 보내기. context를 유지해야하므로 별도로 response Delegate를 지정한다.
				{
					var respbuffer			= responsePacket.Pack();	// 바이너리 데이터로 변환

					var resp				= context.Response;
					resp.StatusCode			= (int)HttpStatusCode.OK;
					resp.ContentType		= "application/octet-stream";
					resp.ContentLength64	= respbuffer.Length;
					resp.OutputStream.Write(respbuffer, 0, respbuffer.Length);	// 바이너리 패킷 정보 전송
					resp.Close();
				});
			});
			
			return bridge;
		}
	}
}

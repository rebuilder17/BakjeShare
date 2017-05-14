using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Threading;
using System.Diagnostics;

namespace BakjeShareServer.Http
{
	class Server
	{
		// Constants

		const int		c_workerThreadCount	= 16;


		// Members

		HttpListener	m_listener;

		Thread			m_threadListener;					// http 연결을 처리하는 쓰레드
		Thread []		m_threadWorkers;					// 각 http연결마다 응답을 만들어내는 쓰레드

		ManualResetEvent	m_reStop;						// 쓰레드 리셋 이벤트 - 서버 정지시
		ManualResetEvent	m_reReady;						// 쓰레드 리셋 이벤트 - worker 쓰레드 준비

		Queue<HttpListenerContext>	m_contextQueue;			// 요청의 context 큐


		public Server()
		{
			m_reStop		= new ManualResetEvent(false);
			m_reReady		= new ManualResetEvent(false);

			m_threadWorkers	= new Thread[c_workerThreadCount];

			m_contextQueue	= new Queue<HttpListenerContext>();
			m_listener		= new HttpListener();
			m_threadListener	= new Thread(DoHandleRequest);
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
						// TODO : 여기에 진짜 요청 처리 코드 넣기

						var request		= context.Request;
						var response	= context.Response;

						Console.Out.WriteLine("request received : " + request.Url.OriginalString);
						var segments	= request.Url.Segments;
						if (segments.Length > 0)
						{
							Console.Out.WriteLine("Segments are : ");
							foreach (var seg in segments)
							{
								Console.Out.WriteLine(seg);
							}
						}
						
						response.StatusCode			= 200;
						response.ContentType		= "text/plain";
						response.ContentEncoding	= Encoding.UTF8;
						var buffer					= Encoding.UTF8.GetBytes("Hello!");
						response.ContentLength64	= buffer.Length;
						response.OutputStream.Write(buffer, 0, buffer.Length);
						response.OutputStream.Close();
						
					}
				}
			}
		}
	}
}

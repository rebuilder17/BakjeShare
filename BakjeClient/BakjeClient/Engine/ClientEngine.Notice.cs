using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BakjeProtocol.Parameters;

namespace BakjeClient.Engine
{
	public partial class ClientEngine
	{
		public interface INotice
		{
			void PostNotice(string title, string desc);
			void DeleteNotice(int noticeID);
			RespLookupNotice LookupNotice(int page = 0);
			RespShowNotice ShowNotice(int noticeid);
		}

		protected class Notice : AutoProcedurePool, INotice
		{
			public override string subURL => "/notice/";

			public Notice(ClientEngine engine) : base(engine)
			{
			}

			protected override void InitParamPairs()
			{
				AddParamPair<ReqPostNotice, EmptyParam>(recvTypeStr: "RespPostNotice");
				AddParamPair<ReqDeleteNotice, EmptyParam>(recvTypeStr: "RespDeleteNotice");
				AddParamPair<ReqLookupNotice, RespLookupNotice>();
				AddParamPair<ReqShowNotice, RespShowNotice>();
			}

			public void PostNotice(string title, string desc)
			{
				DoRequest<ReqPostNotice, EmptyParam>(
					(send) =>
					{
						send.SetParameter(new ReqPostNotice { title = title, desc = desc });
					},
					(recv) =>
					{

					});
			}

			public void DeleteNotice(int noticeID)
			{
				DoRequest<ReqDeleteNotice, EmptyParam>(
					(send) =>
					{
						send.SetParameter(new ReqDeleteNotice { noticeID = noticeID });
					},
					(recv) =>
					{

					});
			}

			public RespLookupNotice LookupNotice(int page = 0)
			{
				RespLookupNotice result = null;

				DoRequest<ReqLookupNotice, RespLookupNotice>(
					(send) =>
					{
						send.SetParameter(new ReqLookupNotice { page = 0, rowperpage = 20 });
					},
					(recv) =>
					{
						result	= recv.param;
					});

				return result;
			}

			public RespShowNotice ShowNotice(int noticeid)
			{
				RespShowNotice result = null;

				DoRequest<ReqShowNotice, RespShowNotice>(
					(send) =>
					{
						send.SetParameter(new ReqShowNotice { noticeID = noticeid });
					},
					(recv) =>
					{
						result = recv.param;
					});

				return result;
			}
		}
	}
}

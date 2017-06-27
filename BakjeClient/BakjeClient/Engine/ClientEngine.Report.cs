using BakjeProtocol.Parameters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BakjeClient.Engine
{
	public partial class ClientEngine
	{
		public interface IReport
		{
			void FileReport_Bug(string shortdesc, string longdesc);
			void FileReport_Posting(string shortdesc, string longdesc, int postid, ReqFileReport.PostReportReason reason);
			void FileReport_User(string shortdesc, string longdesc, string reportID, ReqFileReport.UserReportReason reason);
			RespLookupReport LookupReport(int page = 0);
			RespShowReport ShowReport(int reportid);
			void CloseReport(int reportid);
		}

		protected class Report : AutoProcedurePool, IReport
		{
			public override string subURL => "/report/";

			public Report(ClientEngine engine) : base(engine)
			{
			}

			protected override void InitParamPairs()
			{
				AddParamPair<ReqFileReport, RespFileReport>();
				AddParamPair<ReqLookupReport, RespLookupReport>();
				AddParamPair<ReqShowReport, RespShowReport>();
				AddParamPair<ReqCloseReport, RespCloseReport>();
			}

			public void FileReport_Bug(string shortdesc, string longdesc)
			{
				DoRequest<ReqFileReport, RespFileReport>(
					(send) =>
					{
						send.SetParameter(new ReqFileReport
						{
							type				= ReqFileReport.Type.Bug,
							shortdesc			= shortdesc,
							longdesc			= longdesc,
						});
					},
					(recv) =>
					{

					});
			}

			public void FileReport_Posting(string shortdesc, string longdesc, int postid, ReqFileReport.PostReportReason reason)
			{
				DoRequest<ReqFileReport, RespFileReport>(
					(send) =>
					{
						send.SetParameter(new ReqFileReport
						{
							type				= ReqFileReport.Type.Posting,
							shortdesc			= shortdesc,
							longdesc			= longdesc,
							reportingPostID		= postid,
							postReportReason	= reason,
						});
					},
					(recv) =>
					{

					});
			}

			public void FileReport_User(string shortdesc, string longdesc, string reportID, ReqFileReport.UserReportReason reason)
			{
				DoRequest<ReqFileReport, RespFileReport>(
					(send) =>
					{
						send.SetParameter(new ReqFileReport
						{
							type				= ReqFileReport.Type.User,
							shortdesc			= shortdesc,
							longdesc			= longdesc,
							reportingUserID		= reportID,
							userReportReason	= reason,
						});
					},
					(recv) =>
					{

					});
			}

			public RespLookupReport LookupReport(int page = 0)
			{
				RespLookupReport result = null;

				DoRequest<ReqLookupReport, RespLookupReport>(
					(send) =>
					{
						send.SetParameter(new ReqLookupReport
						{
							page = page,
							rowPerPage = 20,
						});
					},
					(recv) =>
					{
						result = recv.param;
					});

				return result;
			}

			public RespShowReport ShowReport(int reportid)
			{
				RespShowReport result = null;

				DoRequest<ReqShowReport, RespShowReport>(
					(send) =>
					{
						send.SetParameter(new ReqShowReport { reportID = reportid });
					},
					(recv) =>
					{
						result = recv.param;
					});

				return result;
			}

			public void CloseReport(int reportid)
			{
				DoRequest<ReqCloseReport, RespCloseReport>(
					(send) =>
					{
						send.SetParameter(new ReqCloseReport { postID = reportid });
					},
					(recv) =>
					{

					});
			}
		}
	}
}

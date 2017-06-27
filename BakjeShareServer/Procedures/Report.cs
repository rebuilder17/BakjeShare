using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BakjeProtocol.Auth;
using BakjeShareServer.Http;
using BakjeShareServer.SQL;
using BakjeProtocol.Parameters;

namespace BakjeShareServer.Procedures
{
	public class Report : BaseProcedureSet
	{
		public Report(string suburl, Server server, BaseAuthServer authServer, SQLHelper sqlhelper) : base(suburl, server, authServer, sqlhelper)
		{
		}

		protected override void Initialize()
		{
			// 리폿하기
			procedurePool.AddProcedure<ReqFileReport, RespFileReport>("ReqFileReport", "RespFileReport", UserType.Registered,
			(recv, send) =>
			{
				var reporterID	= authServer.GetUserIDFromAuthKey(recv.header.authKey);
				var typestr		= ReqFileReport.ReportTypeToString(recv.param.type);

				sqlHelper.RunSqlSessionWithTransaction((sql) =>
				{
					var cmd	= sql.CreateCommand();
					cmd.CommandText	= @"insert into report(reporterid, report_type, shortdesc, longdesc) 
													values(@reporterid, @report_type, @shortdesc, @longdesc)";
					cmd.Parameters.AddWithValue("@reporterid", reporterID);
					cmd.Parameters.AddWithValue("@report_type", typestr);
					cmd.Parameters.AddWithValue("@shortdesc", recv.param.shortdesc);
					cmd.Parameters.AddWithValue("@longdesc", recv.param.longdesc);
					cmd.ExecuteNonQuery();
					
					if (recv.param.type == ReqFileReport.Type.Posting)						// 포스팅 리폿인 경우, 추가 정보
					{
						var postcmd	= sql.CreateCommand();
						postcmd.CommandText	= @"insert into report_reason_posting(reportid, reasoncode, postingid)
															values(last_insert_id(), @code, @postid)";
						postcmd.Parameters.AddWithValue("@code", recv.param.postReportReason);
						postcmd.Parameters.AddWithValue("@postid", recv.param.reportingPostID);
						postcmd.ExecuteNonQuery();
					}
					else if (recv.param.type == ReqFileReport.Type.User)					// 유저 리폿인 경우, 추가 정보
					{
						var usercmd	= sql.CreateCommand();
						usercmd.CommandText	= @"insert into report_reason_user(reportid, reasoncode, userid)
															values(last_insert_id(), @code, @userid)";
						usercmd.Parameters.AddWithValue("@code", recv.param.userReportReason);
						usercmd.Parameters.AddWithValue("@userid", recv.param.reportingUserID);
						usercmd.ExecuteNonQuery();
					}

					// 응답
					send.header.code	= BakjeProtocol.Packet.Header.Code.OK;
					send.SetParameter(new RespFileReport { status = RespFileReport.Status.OK });

					return true;
				});
			});

			// 리포트 목록
			procedurePool.AddProcedure<ReqLookupReport, RespLookupReport>("ReqLookupReport", "RespLookupReport", UserType.Administrator,
			(recv, send) =>
			{
				var rowperpage	= recv.param.rowPerPage;
				var rowstart	= rowperpage * recv.param.page;

				sqlHelper.RunSqlSession((sql) =>
				{
					var result = new RespLookupReport();

					var cmd	= sql.CreateCommand();
					cmd.CommandText	= @"select SQL_CALC_FOUND_ROWS idreport, reporterid, report_type, shortdesc
												from report
												order by idreport asc
												limit " + string.Format("{0}, {1}", rowstart, rowperpage);

					using (var reader = cmd.ExecuteReader())
					{
						var entries	= new List<RespLookupReport.Entry>();

						while(reader.Read())
						{
							var reportType = ReqFileReport.ReportTypeFromString(reader.GetString("report_type"));

							entries.Add(new RespLookupReport.Entry
							{
								reportID	= reader.GetInt32("idreport"),
								type		= reportType,
								shortdesc	= reader.GetString("shortdesc"),
								reporterID	= reader.GetString("reporterid"),
							});
						}
						result.entries	= entries.ToArray();
						reader.Close();
					}

					// 전체 리포트 갯수
					var countcmd			= sql.CreateCommand();
					countcmd.CommandText	= "select FOUND_ROWS()";
					var totalCount			= (int)(long)countcmd.ExecuteScalar();

					// 페이지 갯수 등 기록
					result.currentPage		= recv.param.page;
					result.totalPage		= totalCount / rowperpage + (totalCount % rowperpage == 0 ? 0 : 1);

					// 응답
					send.header.code		= BakjeProtocol.Packet.Header.Code.OK;
					send.SetParameter(result);
				});
			});

			// 리포트 열기
			procedurePool.AddProcedure<ReqShowReport, RespShowReport>("ReqShowReport", "RespShowReport", UserType.Administrator,
			(recv, send) =>
			{
				sqlHelper.RunSqlSession((sql) =>
				{
					var result	= new RespShowReport();

					var cmd	= sql.CreateCommand();
					cmd.CommandText	= @"select reporterid, report_type, shortdesc, longdesc, postingid, userid, 
												(case report_type
													when 'posting' then rp.reasoncode
													when 'user' then ru.reasoncode
													else null
												end) as reason
											from report left outer join report_reason_posting as rp on idreport = rp.reportid
														left outer join report_reason_user as ru on idreport = ru.reportid
											where idreport = @id";
					cmd.Parameters.AddWithValue("@id", recv.param.reportID);
					
					using (var reader = cmd.ExecuteReader())
					{
						reader.Read();

						result.reporterID	= reader.GetString("reporterid");
						result.type			= ReqFileReport.ReportTypeFromString(reader.GetString("report_type"));
						result.shortdesc	= reader.GetString("shortdesc");
						result.longdesc		= reader.GetString("longdesc");

						if (result.type == ReqFileReport.Type.Posting)
						{
							result.repPostingID		= reader.GetInt32("postingid");
							result.postingRepReason	= (ReqFileReport.PostReportReason)reader.GetInt32("reason");
						}
						else if (result.type == ReqFileReport.Type.User)
						{
							result.repUserID		= reader.GetString("userid");
							result.userRepReason	= (ReqFileReport.UserReportReason)reader.GetInt32("reason");
						}

						reader.Close();
					}

					// 포스팅 리폿인 경우 추가로 제목까지 읽어온다
					if (result.type == ReqFileReport.Type.Posting)
					{
						var postcmd	= sql.CreateCommand();
						postcmd.CommandText	= @"select title from postings where idposting = @postid";
						postcmd.Parameters.AddWithValue("@postid", result.repPostingID);

						using (var reader = postcmd.ExecuteReader())
						{
							reader.Read();

							result.repPostingTitle	= reader.GetString("title");
						}
					}

					// 응답
					send.header.code	= BakjeProtocol.Packet.Header.Code.OK;
					send.SetParameter(result);
				});
			});

			// 리포트 삭제
			procedurePool.AddProcedure<ReqCloseReport, RespCloseReport>("ReqCloseReport", "RespCloseReport", UserType.Administrator,
			(recv, send) =>
			{
				sqlHelper.RunSqlSessionWithTransaction((sql) =>
				{
					var cmd	= sql.CreateCommand();
					cmd.CommandText	= @"delete from report where idreport = @id";
					cmd.Parameters.AddWithValue("@id", recv.param.postID);
					cmd.ExecuteNonQuery();

					// 응답
					send.header.code	= BakjeProtocol.Packet.Header.Code.OK;
					send.SetParameter(new RespCloseReport { status = RespCloseReport.Status.OK });

					return true;
				});
			});
		}
	}
}

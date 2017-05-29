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
				var typestr		= (string)null;
				switch(recv.param.type)
				{
					case ReqFileReport.Type.Posting:
						typestr	= "posting";
						break;
					case ReqFileReport.Type.User:
						typestr	= "user";
						break;
					case ReqFileReport.Type.Bug:
						typestr	= "bug";
						break;
				}

				sqlHelper.RunSqlSessionWithTransaction((sql) =>
				{
					var cmd	= sql.CreateCommand();
					cmd.CommandText	= @"insert into report(reporterid, report_type, shortdesc, longdesc) 
													values(@reporterid, @report_type, @shortdesc, @longdesc)";
					cmd.Parameters.AddWithValue("@reporterid", reporterID);
					cmd.Parameters.AddWithValue("@report_type", typestr);
					cmd.Parameters.AddWithValue("@shortdesc", recv.param.shortdesc);
					cmd.Parameters.AddWithValue("@longdesc", recv.param.shortdesc);
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
						usercmd.Parameters.AddWithValue("@postid", recv.param.reportingUserID);
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
							ReqFileReport.Type reportType = ReqFileReport.Type.Bug;
							var typestr		= reader.GetString("report_type");
							switch(typestr)
							{
								case "posting":
									reportType	= ReqFileReport.Type.Posting;
									break;
								case "user":
									reportType	= ReqFileReport.Type.User;
									break;
								case "bug":
									reportType	= ReqFileReport.Type.Bug;
									break;
							}

							entries.Add(new RespLookupReport.Entry
							{
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
		}
	}
}

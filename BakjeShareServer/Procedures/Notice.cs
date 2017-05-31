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
	public class Notice : BaseProcedureSet
	{
		public Notice(string suburl, Server server, BaseAuthServer authServer, SQLHelper sqlhelper) : base(suburl, server, authServer, sqlhelper)
		{
		}

		protected override void Initialize()
		{
			// 공지 올리기
			procedurePool.AddProcedure<ReqPostNotice, EmptyParam>("ReqPostNotice", "RespPostNotice", UserType.Administrator,
			(recv, send) =>
			{
				sqlHelper.RunSqlSessionWithTransaction((sql) =>
				{
					var cmd	= sql.CreateCommand();
					cmd.CommandText	= @"insert into notice(title, description) values(@title, @desc)";
					cmd.Parameters.AddWithValue("@title", recv.param.title);
					cmd.Parameters.AddWithValue("@desc", recv.param.desc);
					cmd.ExecuteNonQuery();

					// 응답
					send.header.code	= BakjeProtocol.Packet.Header.Code.OK;

					return true;
				});
			});

			// 공지 삭제
			procedurePool.AddProcedure<ReqDeleteNotice, EmptyParam>("ReqDeleteNotice", "RespDeleteNotice", UserType.Administrator,
			(recv, send) =>
			{
				sqlHelper.RunSqlSessionWithTransaction((sql) =>
				{
					var cmd = sql.CreateCommand();
					cmd.CommandText	= @"delete from notice where idnotice = @id";
					cmd.Parameters.AddWithValue("@id", recv.param.noticeID);
					cmd.ExecuteNonQuery();

					// 응답
					send.header.code	= BakjeProtocol.Packet.Header.Code.OK;

					return true;
				});
			});

			// 공지 목록
			procedurePool.AddProcedure<ReqLookupNotice, RespLookupNotice>("ReqLookupNotice", "RespLookupNotice", UserType.Registered,
			(recv, send) =>
			{
				sqlHelper.RunSqlSession((sql) =>
				{
					var result		= new RespLookupNotice();

					var rowperpage	= recv.param.rowperpage;
					var rowstart	= recv.param.page * rowperpage;

					var cmd	= sql.CreateCommand();
					cmd.CommandText	= @"select SQL_CALC_FOUND_ROWS idnotice, title, datetime from notice order by idnotice desc limit "
											+ string.Format("{0}, {1}", rowstart, rowperpage);

					using (var reader = cmd.ExecuteReader())
					{
						var entries		= new List<RespLookupNotice.Entry>();

						while(reader.Read())
						{
							entries.Add(new RespLookupNotice.Entry
							{
								noticeID	= reader.GetInt32("idnotice"),
								title		= reader.GetString("title"),
								datetime	= reader.GetDateTime("datetime"),
							});
						}

						reader.Close();

						result.entries = entries.ToArray();
					}

					// 전체 공지 갯수
					var countcmd			= sql.CreateCommand();
					countcmd.CommandText	= "select FOUND_ROWS()";
					var totalCount			= (int)(long)countcmd.ExecuteScalar();

					// 페이지 갯수 등 기록
					result.currentPage		= recv.param.page;
					result.totalPage		= totalCount / rowperpage + (totalCount % rowperpage == 0 ? 0 : 1);

					// 응답
					send.SetParameter(result);
					send.header.code	= BakjeProtocol.Packet.Header.Code.OK;
				});
			});

			// 공지 열기
			procedurePool.AddProcedure<ReqShowNotice, RespShowNotice>("ReqShowNotice", "RespShowNotice", UserType.Registered,
			(recv, send) =>
			{
				sqlHelper.RunSqlSession((sql) =>
				{
					var result	= new RespShowNotice();

					var cmd	= sql.CreateCommand();
					cmd.CommandText	= @"select title, description, datetime from notice where idnotice = @id";
					cmd.Parameters.AddWithValue("@id", recv.param.noticeID);

					using (var reader = cmd.ExecuteReader())
					{
						reader.Read();

						result.title	= reader.GetString("title");
						result.desc		= reader.GetString("description");
						result.datetime	= reader.GetDateTime("datetime");

						reader.Close();
					}

					// 응답
					send.SetParameter(result);
					send.header.code = BakjeProtocol.Packet.Header.Code.OK;
				});
			});
		}
	}
}

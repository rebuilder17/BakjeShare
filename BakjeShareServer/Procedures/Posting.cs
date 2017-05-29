using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BakjeProtocol.Auth;
using BakjeProtocol.Parameters;
using BakjeShareServer.Http;
using BakjeShareServer.SQL;

namespace BakjeShareServer.Procedures
{
	public class Posting : BaseProcedureSet
	{
		public Posting(string suburl, Server server, BaseAuthServer authServer, SQLHelper sqlhelper) : base(suburl, server, authServer, sqlhelper)
		{
		}

		private static string MakeLikeParam(string[] orig)
		{
			if (orig != null && orig.Length > 0)
			{
				return string.Join("%", "", orig, "");
			}
			return null;
		}

		protected override void Initialize()
		{
			// 포스팅 검색
			procedurePool.AddProcedure<ReqLookupPosting, RespLookupPosting>("ReqLookupPosting", "RespLookupPosting", UserType.Registered,
			(recv, send) =>
			{
				sqlHelper.RunSqlSession((sql) =>
				{
					var kwTitle		= MakeLikeParam(recv.param.keyword_title);
					var kwDesc		= MakeLikeParam(recv.param.keyword_desc);
					var kwTag		= recv.param.keyword_tag;
					var kwUser		= recv.param.keyword_user;

					if (kwTitle == null && kwDesc == null && kwTag == null && string.IsNullOrEmpty(kwUser))	// 아무 조건도 걸리지 않았다면
						kwTitle		= "%";																	// 모든 포스팅이 매칭되도록 한다

					var userID		= authServer.GetUserIDFromAuthKey(recv.header.authKey);
					var rowperpage	= recv.param.rowPerPage;
					var rowstart	= recv.param.page * rowperpage;

					var cmd	= sql.CreateCommand();
					cmd.CommandText	=
					@"select distinct SQL_CALC_FOUND_ROWS idposting, authorid, title, datetime, 
														is_private, postings.is_blinded as post_blinded, user.is_blinded as user_blinded
						from (postings left outer join tags on postings.idposting = tags.postingid)
								left outer join user on postings.authorid = user.iduser
						where (title like @title OR description like @desc OR tags.name like @tag OR authorid = @userid)
								and ((is_private = false AND postings.is_blinded = false AND user.is_blinded = false) OR authorid = @userid)
						order by idposting desc
						limit " + string.Format("{0}, {1}", rowstart, rowperpage);

					var param			= cmd.Parameters;
					param.AddWithValue("@title", kwTitle);
					param.AddWithValue("@desc", kwDesc);
					param.AddWithValue("@tag", kwTag);
					param.AddWithValue("@userid", kwUser);
					param.AddWithValue("@rowstart", rowstart);
					param.AddWithValue("@rowperpage", rowperpage);

					var result			= new RespLookupPosting();
					var resultEntries	= new List<RespLookupPosting.Entry>();
					var reader			= cmd.ExecuteReader();
					var totalCount		= 0;
					
					while(reader.Read())
					{
						//if (reader.GetInt32("category") == 1)				// 전체 레코드 갯수 row를 만난 경우, 레코드 갯수 추출 후 종료
						//{
						//	totalCount	= reader.GetInt32("totalrows");
						//	break;
						//}
						//else
						{													// 일반 레코드 직접 추가
							resultEntries.Add(new RespLookupPosting.Entry
							{
								postID	= reader.GetInt32("idposting"),
								author	= reader.GetString("authorid"),
								title	= reader.GetString("title"),
								postingTime	= reader.GetDateTime("datetime"),
								isPrivate	= reader.GetBoolean("is_private"),
								isBlinded	= reader.GetBoolean("post_blinded") && reader.GetBoolean("user_blinded"),
							});
						}
					}
					reader.Close();

					var countcmd			= sql.CreateCommand();
					countcmd.CommandText	= "select FOUND_ROWS()";
					totalCount				= (int)(long)countcmd.ExecuteScalar();
					
					result.entries		= resultEntries.ToArray();
					result.currentPage	= recv.param.page;
					result.totalPage	= totalCount / rowperpage + (totalCount % rowperpage == 0? 0 : 1);
					

					

					send.SetParameter(result);	// 응답으로 전송할 JSON 만들기

					send.header.code	= BakjeProtocol.Packet.Header.Code.OK;
				});
			});

			// 포스팅 열기
			procedurePool.AddProcedure<ReqShowPosting, RespShowPosting>("ReqShowPosting", "RespShowPosting", UserType.Registered,
			(recv, send) =>
			{
				sqlHelper.RunSqlSession((sql) =>
				{
					var postid		= recv.param.postID;
					var myid		= authServer.GetUserIDFromAuthKey(recv.header.authKey);
					var sendParam	= new RespShowPosting();

					// 본문 가져오기

					var cmd	= sql.CreateCommand();
					cmd.CommandText	= @"select authorid, title, description, sourceurl, datetime, is_private from postings
											where idposting = @postid";
					cmd.Parameters.AddWithValue("@postid", postid);

					using (var reader = cmd.ExecuteReader())
					{
						reader.Read();

						sendParam.author	= reader.GetString("authorid");
						sendParam.title		= reader.GetString("title");
						sendParam.desc		= reader.GetString("description");
						sendParam.sourceURL	= reader.GetString("sourceurl");
						sendParam.datetime	= reader.GetDateTime("datetime");
						sendParam.isPrivate	= reader.GetBoolean("is_private");

						reader.Close();
					}

					// 태그 가져오기
					var tagcmd	= sql.CreateCommand();
					tagcmd.CommandText	= @"select name, (taguserid = @myid) as is_mine from tags where postingid = @postid";
					tagcmd.Parameters.AddWithValue("@myid", myid);
					tagcmd.Parameters.AddWithValue("@postid", postid);

					using (var reader = cmd.ExecuteReader())
					{
						var mytags		= new List<string>();
						var othertags	= new List<string>();
						while(reader.Read())					// 쿼리문으로 생성한 is_mine 플래그에 따라서 분류한다.
						{
							(reader.GetBoolean("is_mine") ? mytags : othertags).Add(reader.GetString("name"));
						}

						sendParam.mytags	= mytags.ToArray();
						sendParam.othertags	= othertags.ToArray();

						reader.Close();
					}

					// 이미지정보 (BLOB) 가져오기

					var imgcmd	= sql.CreateCommand();
					imgcmd.CommandText	= @"select imagedata, length(imagedata) as imagesize from screenshots where postingid = @postid";
					imgcmd.Parameters.AddWithValue("@postid", postid);

					using (var reader = imgcmd.ExecuteReader())
					{
						while(reader.Read())
						{
							var size	= reader.GetInt32("imagesize");
							var data	= new byte[size];
							reader.GetBytes(0, 0, data, 0, size);

							send.AddBinaryData(data);			// 별도 필드에 데이터 추가하기
						}
					}

					send.SetParameter(sendParam);	// 응답으로 전송할 JSON 만들기

					send.header.code	= BakjeProtocol.Packet.Header.Code.OK;
				});
			});


			// 포스팅 업로드

			procedurePool.AddProcedure<ReqNewPosting, RespPostingModify>("ReqNewPosting", "RespNewPosting", UserType.Registered,
			(recv, send) =>
			{
				var userid	= authServer.GetUserIDFromAuthKey(recv.header.authKey);

				sqlHelper.RunSqlSessionWithTransaction((sql) =>
				{
					// 본문 추가

					var cmd	= sql.CreateCommand();
					cmd.CommandText	= @"insert into postings(authorid, title, description, sourceurl, is_private)
														values(@userid, @title, @desc, @url, @isprivate)";
					var param	= cmd.Parameters;
					param.AddWithValue("@userid", userid);
					param.AddWithValue("@title", recv.param.title);
					param.AddWithValue("@desc", recv.param.desc);
					param.AddWithValue("@url", recv.param.sourceURL);
					param.AddWithValue("@isprivate", recv.param.isPrivate);
					cmd.ExecuteNonQuery();

					// 방금 추가한 포스팅 ID 가져오기

					var idcmd	= sql.CreateCommand();
					idcmd.CommandText	= "select last_insert_id()";
					var lastid	= (ulong)idcmd.ExecuteScalar();

					// 스샷 데이터 추가

					var datacount	= recv.binaryDataCount;
					for (var i = 0; i < datacount; i++)
					{
						var imgcmd	= sql.CreateCommand();
						imgcmd.CommandText	= @"insert into screenshots(postingid, imagedata) values(@id, @blob)";
						imgcmd.Parameters.AddWithValue("@id", lastid);
						imgcmd.Parameters.AddWithValue("@blob", recv.GetBinaryData(i));
						imgcmd.ExecuteNonQuery();
					}

					// 상태 리턴
					send.header.code	= BakjeProtocol.Packet.Header.Code.OK;
					send.SetParameter(new RespPostingModify() { status = RespPostingModify.Status.OK, postID = (int)lastid });

					return true;
				});
			});

			// 포스팅 삭제

			procedurePool.AddProcedure<ReqDeletePosting, RespDeletePosting>("ReqDeletePosting", "RespDeletePosting", UserType.Registered,
			(recv, send) =>
			{
				var postid	= recv.param.postID;

				sqlHelper.RunSqlSessionWithTransaction((sql) =>
				{
					var cmd	= sql.CreateCommand();
					cmd.CommandText	= @"delete from postings where idposting = @id";
					cmd.Parameters.AddWithValue("@id", postid);
					cmd.ExecuteNonQuery();

					// 상태 리턴
					send.header.code	= BakjeProtocol.Packet.Header.Code.OK;
					send.SetParameter(new RespDeletePosting() { status = RespDeletePosting.Status.OK });

					return true;
				});
			});

			// 태그 추가

			procedurePool.AddProcedure<ReqAddTag, RespAddTag>("ReqAddTag", "RespAddTag", UserType.Registered,
			(recv, send) =>
			{
				var userid	= authServer.GetUserIDFromAuthKey(recv.header.authKey);

				sqlHelper.RunSqlSessionWithTransaction((sql) =>
				{
					var cmd	= sql.CreateCommand();
					cmd.CommandText	= @"insert into tags(name, postingid, taguserid) values(@tag, @postid, @userid)";
					var param = cmd.Parameters;
					param.AddWithValue("@tag", recv.param.tagname);
					param.AddWithValue("@postid", recv.param.postID);
					param.AddWithValue("@userid", userid);
					cmd.ExecuteNonQuery();

					// 상태 리턴
					send.header.code	= BakjeProtocol.Packet.Header.Code.OK;
					send.SetParameter(new RespAddTag() { status = RespAddTag.Status.OK });

					return true;
				});
			});

			// 태그 제거

			procedurePool.AddProcedure<ReqDeleteTag, RespDeleteTag>("ReqDeleteTag", "RespDeleteTag", UserType.Registered,
			(recv, send) =>
			{
				var userid	= authServer.GetUserIDFromAuthKey(recv.header.authKey);

				sqlHelper.RunSqlSessionWithTransaction((sql) =>
				{
					var cmd	= sql.CreateCommand();
					cmd.CommandText	= @"delete from tags where name = @tag and postingid = @postid and taguserid = @userid";
					var param	= cmd.Parameters;
					param.AddWithValue("@postid", recv.param.postID);
					param.AddWithValue("@tag", recv.param.tagname);
					param.AddWithValue("@userid", userid);
					cmd.ExecuteNonQuery();

					// 상태 리턴
					send.header.code	= BakjeProtocol.Packet.Header.Code.OK;
					send.SetParameter(new RespDeleteTag() { status = RespDeleteTag.Status.OK });

					return true;
				});
			});
		}
	}
}

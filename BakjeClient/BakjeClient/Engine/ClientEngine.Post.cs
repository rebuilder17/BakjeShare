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
		public interface IPost
		{
			void NewPost(string title, string description, string sourceURL, bool isPrivate, IList<byte[]> dataList = null);
			void DeletePost(int postID);
			void BlindPost(int postID, bool setblind);
			void AddTag(int postID, string tag);
			void DeleteTag(int postID, string tag);
			RespLookupPosting LookupPosting(PostingLookupCondition condition, int page = 0, int rowperpage = 20);
			PostingDetail ShowPosting(int postid);
		}

		public class PostingLookupCondition
		{
			public static readonly PostingLookupCondition empty = new PostingLookupCondition();

			public string title;
			public string desc;
			public string user;
			public string tag;
		}

		public class PostingDetail
		{
			public RespShowPosting		postingInfo;
			public ICollection<byte[]>	imageArray;
		}

		protected class Post : AutoProcedurePool, IPost
		{
			public override string subURL => "/posting/";

			public Post(ClientEngine engine) : base(engine)
			{
			}

			protected override void InitParamPairs()
			{
				AddParamPair<ReqLookupPosting, RespLookupPosting>();
				AddParamPair<ReqShowPosting, RespShowPosting>();
				AddParamPair<ReqNewPosting, RespPostingModify>(recvTypeStr: "RespNewPosting");
				AddParamPair<ReqDeletePosting, RespDeletePosting>();
				AddParamPair<ReqBlindPosting, EmptyParam>(recvTypeStr:"RespBlindPosting");
				AddParamPair<ReqAddTag, RespAddTag>();
				AddParamPair<ReqDeleteTag, RespDeleteTag>();
			}

			public void NewPost(string title, string description, string sourceURL, bool isPrivate, IList<byte[]> dataList = null)
			{
				DoRequest<ReqNewPosting, RespPostingModify>(
					(send) =>
					{
						send.SetParameter(new ReqNewPosting
						{
							title		= title,
							desc		= description,
							sourceURL	= sourceURL,
							isPrivate	= isPrivate,
						});

						if (dataList != null)
						{
							foreach (var data in dataList)
							{
								send.AddBinaryData(data);
							}
						}
					},
					(recv) =>
					{
						//postID = recv.param.postID;
					});
			}

			public void DeletePost(int postID)
			{
				DoRequest<ReqDeletePosting, RespDeletePosting>(
					(send) =>
					{
						send.SetParameter(new ReqDeletePosting { postID = postID });
					},
					(recv) =>
					{

					});
			}

			public void BlindPost(int postID, bool setblind)
			{
				DoRequest<ReqBlindPosting, EmptyParam>(
					(send) =>
					{
						send.SetParameter(new ReqBlindPosting { postID = postID, setBlind = setblind });
					},
					(recv) =>
					{

					});
			}

			public void AddTag(int postID, string tag)
			{
				DoRequest<ReqAddTag, RespAddTag>(
					(send) =>
					{
						send.SetParameter(new ReqAddTag { postID = postID, tagname = tag });
					},
					(recv) =>
					{

					});
			}

			public void DeleteTag(int postID, string tag)
			{
				DoRequest<ReqDeleteTag, RespDeleteTag>(
					(send) =>
					{
						send.SetParameter(new ReqDeleteTag { postID = postID, tagname = tag });
					},
					(recv) =>
					{

					});
			}

			public RespLookupPosting LookupPosting(PostingLookupCondition condition, int page = 0, int rowperpage = 20)
			{
				RespLookupPosting result = null;

				condition = condition ?? PostingLookupCondition.empty;

				DoRequest<ReqLookupPosting, RespLookupPosting>(
					(send) =>
					{
						send.SetParameter(new ReqLookupPosting
						{
							keyword_title	= condition.title?.Split(),
							keyword_desc	= condition.desc?.Split(),
							keyword_tag		= condition.tag,
							keyword_user	= condition.user,
							page			= page,
							rowPerPage		= rowperpage,
						});
					},
					(recv) =>
					{
						result	= recv.param;
					});

				return result;
			}

			public PostingDetail ShowPosting(int postid)
			{
				PostingDetail result = null;

				DoRequest<ReqShowPosting, RespShowPosting>(
					(send) =>
					{
						send.SetParameter(new ReqShowPosting { postID = postid });
					},
					(recv) =>
					{
						if (recv.param != null)
						{
							result				= new PostingDetail();
							result.postingInfo	= recv.param;

							var imageList		= new List<byte[]>();
							for (var i = 0; i < recv.binaryDataCount; i++)
							{
								imageList.Add(recv.GetBinaryData(i));
							}

							result.imageArray	= imageList;
						}
						else
						{

						}
					});

				return result;
			}
		}
	}
}

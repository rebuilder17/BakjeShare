using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BakjeProtocol.Auth;
using BakjeShareServer.Http;
using BakjeShareServer.SQL;

namespace BakjeShareServer.Procedures
{
	public class Posting : BaseProcedureSet
	{
		public Posting(string suburl, Server server, BaseAuthServer authServer, SQLHelper sqlhelper) : base(suburl, server, authServer, sqlhelper)
		{
		}

		protected override void Initialize()
		{
			
		}
	}
}

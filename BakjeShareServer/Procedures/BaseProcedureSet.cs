using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BakjeProtocol;

namespace BakjeShareServer.Procedures
{
	public abstract class BaseProcedureSet
	{
		protected ServerProcedurePool	procedurePool { get; private set; }
		protected SQL.SQLHelper			sqlHelper { get; private set; }
		protected BakjeProtocol.Auth.BaseAuthServer authServer { get; private set; }

		public BaseProcedureSet(string suburl, Http.Server server, BakjeProtocol.Auth.BaseAuthServer authServer, SQL.SQLHelper sqlhelper)
		{
			procedurePool	= new ServerProcedurePool();
			procedurePool.SetBridge(server.CreateProcedurePoolBridge(suburl));
			procedurePool.SetAuthServerObj(authServer);

			sqlHelper		= sqlhelper;
			this.authServer	= authServer;

			Initialize();

			procedurePool.Start();
		}

		protected abstract void Initialize();
	}
}

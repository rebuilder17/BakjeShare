using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BakjeProtocol.Auth;
using System.Threading;

namespace BakjeShareServer.SQL
{
	/// <summary>
	/// SQL 쿼리를 통해 auth키를 관리하는 클래스
	/// </summary>
	public class SQLAuthServer : BaseAuthServer
	{
		// Members

		SQLHelper		m_sql;
		int				m_keyGenCount;
		
		
		public SQLAuthServer(SQLHelper sqlHelper)
		{
			m_sql			= sqlHelper;
			m_keyGenCount	= 0;
		}

		protected override string GenerateAuthKey(string userid)
		{
			var keyGenCount	= Interlocked.Increment(ref m_keyGenCount);		// 4 bytes
			var curTime		= DateTime.Now.Ticks;							// 8 bytes

			var buffer		= new byte[24];
			Buffer.BlockCopy(BitConverter.GetBytes(keyGenCount), 0, buffer, 0, 4);
			Buffer.BlockCopy(BitConverter.GetBytes(curTime), 0, buffer, 4, 8);

			var idbytes		= Encoding.ASCII.GetBytes(userid);				// 최대 12바이트까지
			var idlenlimit	= 12;
			var idlen		= idbytes.Length > idlenlimit? idlenlimit : idbytes.Length;
			Buffer.BlockCopy(idbytes, 0, buffer, 12, idlen);
			
			return Convert.ToBase64String(buffer);
		}

		protected override string QueryAuthKey(string userid)
		{
			var authkey = (string)null;

			m_sql.RunSqlSession((bridge) =>
			{
				var cmd	= bridge.CreateCommand();
				cmd.CommandText	= "select authkey from authkeys where iduser = @id";
				cmd.Parameters.AddWithValue("@id", userid);
				var reader	= cmd.ExecuteReader();

				if (reader.HasRows)
				{
					reader.Read();
					authkey	= reader.GetString("authkey");
				}

				reader.Close();
			});

			return authkey;
		}

		protected override void QuerySetAuthInfo(string userid, string authkey, UserType usertype)
		{
			m_sql.RunSqlSessionWithTransaction((bridge) =>
			{
				var cmd	= bridge.CreateCommand();
				cmd.CommandText	= "insert into authkeys(iduser, authkey) values(@id, @key) on duplicate key update authkey = @key";
				cmd.Parameters.AddWithValue("@id", userid);
				cmd.Parameters.AddWithValue("@key", authkey);
				cmd.ExecuteNonQuery();

				return true;
			});
		}

		protected override string QueryUserID(string authkey)
		{
			var userid = (string)null;

			m_sql.RunSqlSession((bridge) =>
			{
				var cmd	= bridge.CreateCommand();
				cmd.CommandText	= "select authkey from iduser where authkey = @key";
				cmd.Parameters.AddWithValue("@key", authkey);
				var reader	= cmd.ExecuteReader();

				if (reader.HasRows)
				{
					reader.Read();
					userid	= reader.GetString("iduser");
				}

				reader.Close();
			});

			return userid;
		}

		protected override UserType QueryUserType(string userid)
		{
			UserType utype	= UserType.Guest;

			m_sql.RunSqlSession((bridge) =>
			{
				var cmd	= bridge.CreateCommand();
				cmd.CommandText	= "select is_admin from user where iduser = @id";
				cmd.Parameters.AddWithValue("@id", userid);
				var reader	= cmd.ExecuteReader();

				if (reader.HasRows)
				{
					reader.Read();
					var isadmin	= reader.GetBoolean("is_admin");
					utype	= isadmin ? UserType.Administrator : UserType.Registered;
				}

				reader.Close();
			});

			return utype;
		}
	}
}

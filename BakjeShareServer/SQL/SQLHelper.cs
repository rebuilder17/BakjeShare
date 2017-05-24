using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data;
using MySql.Data.Entity;
using MySql.Data.MySqlClient;

namespace BakjeShareServer.SQL
{
	/// <summary>
	/// MySQL 객체를 좀더 편하게 다루게 하기 위한 클래스
	/// </summary>
	public class SQLHelper
	{
		public interface ISqlCommandBridge
		{
			/// <summary>
			/// 적절하게 MySqlCommand 객체를 생성해서 리턴한다.
			/// </summary>
			/// <returns></returns>
			MySqlCommand CreateCommand();
		}

		class SqlCommandBridge : ISqlCommandBridge
		{
			MySqlConnection		m_con;
			MySqlTransaction	m_trans;

			public SqlCommandBridge(MySqlConnection con, MySqlTransaction trans = null)
			{
				m_con	= con;
				m_trans	= trans;
			}

			public MySqlCommand CreateCommand()
			{
				var com	= m_con.CreateCommand();
				if (m_trans != null)
					com.Transaction	= m_trans;

				return com;
			}
		}


		// Members

		public string	serverURL { get; private set; }
		public string	sqlUser	{ get; private set; }
		public string	sqlPassword { get; private set; }
		public string	sqlDatabaseName { get; private set; }


		string		m_connectionString;
		
		/// <summary>
		/// SQL 연결 문자열을 설정한다.
		/// </summary>
		/// <param name="url"></param>
		/// <param name="user"></param>
		/// <param name="password"></param>
		/// <param name="dbname"></param>
		public void SetupConnectionString(string url, string user, string password, string dbname)
		{
			serverURL		= url;
			sqlUser			= user;
			sqlPassword		= password;
			sqlDatabaseName	= dbname;

			m_connectionString	= string.Format("Server={0};Uid={1};Pwd={2};Database={3};MinimumPoolSize=1;", url, user, password, dbname);
		}

		/// <summary>
		/// SQL 연결 후 델리게이트에 필요한 정보를 주고 조작할 수 있게 실행한다. 실행 후에는 SQL 연결을 자동으로 정리한다.
		/// </summary>
		/// <param name="del"></param>
		public void RunSqlSession(Action<ISqlCommandBridge> func)
		{
			using (var connection	= new MySqlConnection(m_connectionString))
			{
				connection.Open();
				var bridge = new SqlCommandBridge(connection);

				func(bridge);
			}
		}

		/// <summary>
		/// SQL 연결 후 델리게이트에 필요한 정보를 주고 조작할 수 있게 한다. Transaction 관리를 한다.
		/// 에러가 발생하거나 델리게이트가 false를 리턴하면 rollback, 델리게이트가 true를 리턴하면 commit한다.
		/// </summary>
		/// <param name="func"></param>
		public void RunSqlSessionWithTransaction(Func<ISqlCommandBridge, bool> func)
		{
			using (var connection	= new MySqlConnection(m_connectionString))
			{
				connection.Open();
				var transaction	= connection.BeginTransaction();
				var bridge		= new SqlCommandBridge(connection, transaction);

				try
				{
					var doCommit	= func(bridge);				// 델리게이트를 실행하고 commit 여부를 리턴값으로 받는다.

					if (doCommit)
					{
						transaction.Commit();
					}
					else
					{
						transaction.Rollback();
					}
				}
				catch (Exception e)
				{
					transaction.Rollback();						// Exception이 발생하면 트랜잭션은 rollback한다
					throw e;									// Exception을 다시 외부로 토스
				}
			}
		}
	}
}

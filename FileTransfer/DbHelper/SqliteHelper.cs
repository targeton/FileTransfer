using log4net;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileTransfer.DbHelper
{
    public class SqliteHelper
    {
        #region 变量
        private static ILog _logger = LogManager.GetLogger(typeof(SqliteHelper));
        private string _connectStr = string.Empty;
        #endregion

        #region 属性
        public string ConnectStr { get { return _connectStr; } }
        #endregion

        #region 单例
        private static SqliteHelper _instance;

        public static SqliteHelper Instance
        {
            get { return _instance ?? (_instance = new SqliteHelper()); }
        }

        #endregion

        #region 构造函数
        public SqliteHelper()
        {
            string exePath = System.Environment.CurrentDirectory;
            string dbPath = System.IO.Path.Combine(exePath, "Database", "log.db");
            _connectStr = string.Format("Data Source = {0};PRAGMA journal_mode = WAL", dbPath);
        }

        public void CreateDatabase()
        {
            string createSendLogSql = @"CREATE TABLE IF NOT EXISTS SendLog(ID INTEGER PRIMARY KEY AUTOINCREMENT,SendDate DATETIME NOT NULL,SendFile TEXT NOT NULL,SubscribeIP NVARCHAR(30) NOT NULL,SendState NVARCHAR(10) NOT NULL);";
            string createReceiveLogSql = @"CREATE TABLE IF NOT EXISTS ReceiveLog(ID INTEGER PRIMARY KEY AUTOINCREMENT,ReceiveDate DATETIME NOT NULL,ReceiveFile TEXT NOT NULL,MonitorIP NVARCHAR(30) NOT NULL,MonitorAlias TEXT NOT NULL,ReceiveState NVARCHAR(10) NOT NULL);";
            string createMonitorLogSql = @"CREATE TABLE IF NOT EXISTS MonitorLog(ID INTEGER PRIMARY KEY AUTOINCREMENT,MonitorDate DATETIME NOT NULL,ChangedFile TEXT NOT NULL);";
            string createErrorLogSql = @"CREATE TABLE IF NOT EXISTS ErrorLog(ID INTEGER PRIMARY KEY AUTOINCREMENT,LogDate DATETIME NOT NULL,LogLevel NVARCHAR(10) NOT NULL,LogMessage TEXT NOT NULL);";
            string createTableSql = createSendLogSql + createReceiveLogSql + createMonitorLogSql + createErrorLogSql;
            using (SQLiteConnection conn = new SQLiteConnection(_connectStr))
            {
                try
                {
                    conn.Open();
                    DbTransaction trans = conn.BeginTransaction();
                    try
                    {
                        DbCommand command = conn.CreateCommand();
                        command.CommandText = createTableSql;
                        command.ExecuteNonQuery();
                        trans.Commit();
                    }
                    catch (Exception e)
                    {
                        trans.Rollback();
                        _logger.Error(string.Format("sqlite3数据库中建表时发生异常，异常：{0}", e.Message));
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error(string.Format("创建sqlite3数据库时发生异常，异常：{0}", ex.Message));
                    throw;
                }
            }
        }
        #endregion

    }
}

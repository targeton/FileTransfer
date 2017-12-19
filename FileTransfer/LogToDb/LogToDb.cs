using FileTransfer.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileTransfer.LogToDb
{
    public class LogToSQLiteDb<T> : ProducerConsumerLite<T>
    {
        #region 变量
        private string _connectStr = string.Empty;
        #endregion

        #region 构造函数
        public LogToSQLiteDb()
        {
            _bufferSize = 100;
            string exePath = System.Environment.CurrentDirectory;
            string dbPath = System.IO.Path.Combine(exePath, "Database", "log.db");
            _connectStr = string.Format("Data Source = {0};PRAGMA journal_mode = WAL", dbPath);
        }
        #endregion

        #region 方法
        protected override void Consume(IEnumerable<T> items)
        {
            InsertRowsToDb(items);
        }

        private void InsertRowsToDb(IEnumerable<T> rows)
        {
            using (SQLiteConnection conn = new SQLiteConnection(_connectStr))
            {
                try
                {
                    conn.Open();
                    //lock (_lockObject)
                    //{
                    DbTransaction trans = conn.BeginTransaction();
                    try
                    {
                        BatchInsertRows(conn, rows);
                        trans.Commit();
                    }
                    catch (Exception)
                    {
                        trans.Rollback();
                        throw;
                    }
                    //}
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }

        protected virtual void BatchInsertRows(DbConnection conn, IEnumerable<T> rows)
        { }

        #endregion

    }
}

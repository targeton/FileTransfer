using FileTransfer.Utils;
using System;
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
        private int _batchSize = 0;
        private string _connectStr = string.Empty;
        private List<T> _cacheList = new List<T>();
        #endregion

        #region 构造函数
        public LogToSQLiteDb()
        {
            _batchSize = 20;
            string exePath = System.Environment.CurrentDirectory;
            string dbPath = System.IO.Path.Combine(exePath, "Database", "log.db");
            _connectStr = string.Format("Data Source={0}", dbPath);
        }
        #endregion

        #region 方法
        protected override void Consume(T item)
        {
            if (_cacheList.Count < _batchSize)
            {
                _cacheList.Add(item);
                return;
            }
            InsertRowsToDb(_cacheList);
            _cacheList = new List<T>();
        }

        protected override void BeforeTaskEnd()
        {
            if (_cacheList.Count <= 0) return;
            InsertRowsToDb(_cacheList);
            _cacheList = new List<T>();
        }

        private void InsertRowsToDb(List<T> rows)
        {
            using (SQLiteConnection conn = new SQLiteConnection(_connectStr))
            {
                try
                {
                    conn.Open();
                    DbTransaction trans = conn.BeginTransaction();
                    try
                    {
                        BatchInsertRows(conn, rows);
                    }
                    catch (Exception)
                    {
                        trans.Rollback();
                        throw;
                    }
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }

        protected virtual void BatchInsertRows(DbConnection conn, List<T> rows)
        { }

        #endregion

    }
}

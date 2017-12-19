using FileTransfer.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileTransfer.LogToDb
{
    public class ErrorLogger : LogToSQLiteDb<ErrorLogEntity>
    {
        #region 方法
        protected override void BatchInsertRows(System.Data.Common.DbConnection conn, IEnumerable<ErrorLogEntity> rows)
        {
            DbCommand command = conn.CreateCommand();
            command.CommandText = @"INSERT INTO ErrorLog(LogDate,LogLevel,LogMessage) VALUES(@LogDate,@LogLevel,@LogMessage)";
            DbParameter dateParam = new SQLiteParameter("@LogDate", DbType.DateTime);
            DbParameter levelParam = new SQLiteParameter("@LogLevel", DbType.String, 10);
            DbParameter messageParam = new SQLiteParameter("@LogMessage", DbType.String);
            command.Parameters.Add(dateParam);
            command.Parameters.Add(levelParam);
            command.Parameters.Add(messageParam);
            foreach (var r in rows)
            {
                dateParam.Value = r.LogDate;
                levelParam.Value = r.LogLevel;
                messageParam.Value = r.LogMessage;
                command.ExecuteNonQuery();
            }
        }
        #endregion
    }
}

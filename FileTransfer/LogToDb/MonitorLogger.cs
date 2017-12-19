using FileTransfer.DbHelper.Entitys;
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
    public class MonitorLogger : LogToSQLiteDb<MonitorLogEntity>
    {
        #region 方法
        protected override void BatchInsertRows(System.Data.Common.DbConnection conn, IEnumerable<MonitorLogEntity> rows)
        {
            DbCommand command = conn.CreateCommand();
            command.CommandText = @"INSERT INTO MonitorLog(MonitorDate,ChangedFile) VALUES(@MonitorDate,@ChangedFile)";
            DbParameter dateParam = new SQLiteParameter("@MonitorDate", DbType.DateTime);
            DbParameter fileParam = new SQLiteParameter("@ChangedFile", DbType.String);
            command.Parameters.Add(dateParam);
            command.Parameters.Add(fileParam);
            foreach (var r in rows)
            {
                dateParam.Value = r.MonitorDate;
                fileParam.Value = r.ChangedFile;
                command.ExecuteNonQuery();
            }
        }
        #endregion
    }
}

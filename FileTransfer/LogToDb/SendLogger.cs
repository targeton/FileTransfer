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
    public class SendLogger : LogToSQLiteDb<SendLogEntity>
    {
        #region 方法
        protected override void BatchInsertRows(System.Data.Common.DbConnection conn, IEnumerable<SendLogEntity> rows)
        {
            DbCommand command = conn.CreateCommand();
            command.CommandText = @"INSERT INTO SendLog(SendDate,SendFile,SubscribeIP,SendState) VALUES(@SendDate,@SendFile,@SubscribeIP,@SendState)";
            DbParameter dateParam = new SQLiteParameter("@SendDate", DbType.DateTime);
            DbParameter fileParam = new SQLiteParameter("@SendFile", DbType.String);
            DbParameter ipParam = new SQLiteParameter("@SubscribeIP", DbType.String, 30);
            DbParameter stateParam = new SQLiteParameter("@SendState", DbType.String, 10);
            command.Parameters.Add(dateParam);
            command.Parameters.Add(fileParam);
            command.Parameters.Add(ipParam);
            command.Parameters.Add(stateParam);
            foreach (var r in rows)
            {
                dateParam.Value = r.SendDate;
                fileParam.Value = r.SendFile;
                ipParam.Value = r.SubscribeIP;
                stateParam.Value = r.SendState;
                command.ExecuteNonQuery();
            }
        }
        #endregion
    }
}

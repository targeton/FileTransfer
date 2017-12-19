using NHibernate.Mapping.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileTransfer.DbHelper.Entitys
{
    [Class(Table = "ErrorLog")]

    public class ErrorLogEntity
    {
        [Id(Name = "ID", TypeType = typeof(long), Column = "ID")]
        [Key(1)]
        [Generator(2, Class = "assigned")]
        public virtual long ID { get; set; }
        [Property(Column = "LogDate", TypeType = typeof(DateTime))]
        public virtual DateTime LogDate { get; set; }
        [Property(Column = "LogLevel", Length = 10, TypeType = typeof(string))]
        public virtual string LogLevel { get; set; }
        [Property(Column = "LogMessage", TypeType = typeof(string))]
        public virtual string LogMessage { get; set; }

        public ErrorLogEntity()
        { }

        public ErrorLogEntity(DateTime logDate, string logLevel, string logMessage)
        {
            LogDate = logDate;
            LogLevel = logLevel;
            LogMessage = logMessage;
        }
    }
}

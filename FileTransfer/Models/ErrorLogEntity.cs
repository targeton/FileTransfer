using NHibernate.Mapping.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileTransfer.Models
{
    [Class(Table = "ErrorLog")]
    public class ErrorLogEntity
    {
        [Id(Name = "LogDate", TypeType = typeof(DateTime), Column = "LogDate")]
        public virtual DateTime LogDate { get; set; }
        [Property(Column = "LogLevel", Length = 10, TypeType = typeof(string))]
        public virtual string LogLevel { get; set; }
        [Property(Column = "LogMessage", TypeType = typeof(string))]
        public virtual string LogMessage { get; set; }
    }
}

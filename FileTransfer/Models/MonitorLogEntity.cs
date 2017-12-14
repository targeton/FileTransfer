using NHibernate.Mapping.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileTransfer.Models
{
    [Class(Table = "MonitorLog")]
    public class MonitorLogEntity
    {
        [Id(Name = "MonitorDate", TypeType = typeof(DateTime), Column = "MonitorDate")]
        public virtual DateTime MonitorDate { get; set; }
        [Property(Column = "ChangedFile", TypeType = typeof(string))]
        public virtual string ChangedFile { get; set; }
    }
}

using NHibernate.Mapping.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileTransfer.Models
{
    [Class(Table = "ReceiveLog")]
    public class ReceiveLogEntity
    {
        [Id(Name = "ID", TypeType = typeof(long), Column = "ID")]
        [Key(1)]
        [Generator(2, Class = "assigned")]
        public virtual long ID { get; set; }
        [Property(TypeType = typeof(DateTime), Column = "ReceiveDate")]
        public virtual DateTime ReceiveDate { get; set; }
        [Property(TypeType = typeof(string), Column = "ReceiveFile")]
        public virtual string ReceiveFile { get; set; }
        [Property(TypeType = typeof(string), Column = "MonitorIP")]
        public virtual string MonitorIP { get; set; }
        [Property(TypeType = typeof(string), Column = "MonitorDirectory")]
        public virtual string MonitorDirectory { get; set; }
        [Property(TypeType = typeof(string), Column = "ReceiveState")]
        public virtual string ReceiveState { get; set; }

        public ReceiveLogEntity()
        { }

        public ReceiveLogEntity(DateTime receiveTime, string receiveFile, string monitoIP, string monitorDirectory, string receiveState)
        {
            ReceiveDate = receiveTime;
            ReceiveFile = receiveFile;
            MonitorIP = monitoIP;
            MonitorDirectory = monitorDirectory;
            ReceiveState = receiveState;
        }

    }
}

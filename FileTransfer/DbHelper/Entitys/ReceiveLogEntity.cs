using NHibernate.Mapping.Attributes;
using System;

namespace FileTransfer.DbHelper.Entitys
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
        [Property(TypeType = typeof(string), Column = "MonitorAlias")]
        public virtual string MonitorAlias { get; set; }
        [Property(TypeType = typeof(string), Column = "ReceiveState")]
        public virtual string ReceiveState { get; set; }

        public ReceiveLogEntity()
        { }

        public ReceiveLogEntity(DateTime receiveTime, string receiveFile, string monitoIP, string monitorAlias, string receiveState)
        {
            ReceiveDate = receiveTime;
            ReceiveFile = receiveFile;
            MonitorIP = monitoIP;
            MonitorAlias = monitorAlias;
            ReceiveState = receiveState;
        }

    }
}

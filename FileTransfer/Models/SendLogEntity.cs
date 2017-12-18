using NHibernate.Mapping.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileTransfer.Models
{
    [Class(Table = "SendLog")]
    public class SendLogEntity
    {
        [Id(Name = "ID", TypeType = typeof(long), Column = "ID")]
        [Key(1)]
        [Generator(2, Class = "assigned")]
        public virtual long ID { get; set; }
        [Property(TypeType = typeof(DateTime), Column = "SendDate")]
        public virtual DateTime SendDate { get; set; }
        [Property(TypeType = typeof(string), Column = "SendFile")]
        public virtual string SendFile { get; set; }
        [Property(TypeType = typeof(string), Column = "SubscribeIP")]
        public virtual string SubscribeIP { get; set; }
        [Property(TypeType = typeof(string), Column = "SendState")]
        public virtual string SendState { get; set; }

        public SendLogEntity()
        { }

        public SendLogEntity(DateTime sendDate, string sendFile, string subscribeIP, string sendState)
        {
            SendDate = sendDate;
            SendFile = sendFile;
            SubscribeIP = subscribeIP;
            SendDate = sendDate;
        }

    }
}

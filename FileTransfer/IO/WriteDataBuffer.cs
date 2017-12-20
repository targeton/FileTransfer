using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileTransfer.IO
{
    public class WriteDataBuffer
    {
        public WriteDataType DataType { get; set; }
        public byte[] DataBuffer { get; set; }
    }
}

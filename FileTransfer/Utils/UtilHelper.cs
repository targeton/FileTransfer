using System.IO;
using System.Net;

namespace FileTransfer.Utils
{
    public class UtilHelper
    {
        #region 单例
        private static UtilHelper _instance;

        public static UtilHelper Instance
        {
            get { return _instance ?? (_instance = new UtilHelper()); }
        }

        #endregion

        #region 方法
        public byte[] GetIPAddressBytes(string ip)
        {
            byte[] address = new byte[4];
            string[] strs = ip.Split('.');
            for (int i = 0; i < 4; i++)
            {
                address[i] = byte.Parse(strs[i]);
            }
            return address;
        }

        /// <summary>
        /// 获取字符串对应的IPEndPoint
        /// </summary>
        /// <param name="ipStr">形如"192.168.12.10:8080"的IP字符串</param>
        /// <returns></returns>
        public IPEndPoint GetIPEndPoint(string ipStr)
        {
            string[] strs = ipStr.Split(':');
            byte[] ipBytes = GetIPAddressBytes(strs[0]);
            int port = int.Parse(strs[1]);
            return new IPEndPoint(new IPAddress(ipBytes), port);
        }

        public long GetFileSize(string fileName)
        {
            FileInfo info = new FileInfo(fileName);
            return info.Length;
        }
        #endregion

    }
}

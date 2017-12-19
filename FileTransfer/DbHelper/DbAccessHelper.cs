using FileTransfer.DbHelper.Entitys;
using FileTransfer.LogToDb;
using FileTransfer.Models;
using log4net;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Mapping.Attributes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileTransfer.DbHelper
{
    public class DbAccessHelper
    {
        private static ILog _logger = LogManager.GetLogger(typeof(DbAccessHelper));

        private static ISessionFactory _sessionFactory;

        public static ISessionFactory SessionFactory
        {
            get
            {
                if (_sessionFactory == null)
                {
                    ConfigSessionFactory();
                }
                return _sessionFactory;
            }
        }

        private static void ConfigSessionFactory()
        {
            try
            {
                var config = new Configuration();
                string cfgFile = Path.Combine(System.Environment.CurrentDirectory, "Configs", "sqlite.cfg.xml");
                config.Configure(cfgFile);
                using (MemoryStream ms = new MemoryStream())
                {
                    HbmSerializer.Default.Serialize(ms, typeof(SendLogEntity));
                    ms.Position = 0;
                    config.AddInputStream(ms);
                }
                using (MemoryStream ms = new MemoryStream())
                {
                    HbmSerializer.Default.Serialize(ms, typeof(ReceiveLogEntity));
                    ms.Position = 0;
                    config.AddInputStream(ms);
                }
                using (MemoryStream ms = new MemoryStream())
                {
                    HbmSerializer.Default.Serialize(ms, typeof(MonitorLogEntity));
                    ms.Position = 0;
                    config.AddInputStream(ms);
                }
                using (MemoryStream ms = new MemoryStream())
                {
                    HbmSerializer.Default.Serialize(ms, typeof(ErrorLogEntity));
                    ms.Position = 0;
                    config.AddInputStream(ms);
                }
                _sessionFactory = config.BuildSessionFactory();
            }
            catch (HibernateException e)
            {
                string msg = string.Format("使用NHibernate框架时，发生异常{0}", e.Message);
                _logger.Warn(msg);
                LogHelper.Instance.ErrorLogger.Add(new ErrorLogEntity(DateTime.Now, "WARN", msg));
            }
        }
    }
}

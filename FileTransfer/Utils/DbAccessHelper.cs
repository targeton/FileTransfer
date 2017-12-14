using FileTransfer.Models;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Mapping.Attributes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileTransfer.Utils
{
    public class DbAccessHelper
    {
        private static ISessionFactory _sessionFactory;

        public static ISessionFactory SessionFactory
        {
            get
            {
                try
                {
                    if (_sessionFactory == null)
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
                }
                catch (HibernateException e)
                {
                    LogHelper.Instance.Logger.Warn(string.Format("使用NHibernate框架时，发生异常{0}", e.Message));
                }
                return _sessionFactory;
            }
        }
    }
}

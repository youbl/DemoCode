
using System;
using System.Collections.Generic;
using System.Xml;
using Beinet.Core.EnumExt;
using Beinet.Core.Xml;

namespace OPJobs.MySql
{
    static class LoadDbConfig
    {
        public static List<DbItem> GetConfigFromFile(string filename)
        {
            return XmlHelper.GetListFromFile(filename, CreateDbItem);
        }

        static DbItem CreateDbItem(XmlNode configNode)
        {
            var dbType = configNode.Name.ToLower();
            var atts = configNode.Attributes;
            if (atts == null)
            {
                return null;
            }

            var server = atts["Server"]?.Value;
            var strport = atts["Port"]?.Value;
            if (string.IsNullOrEmpty(strport) || !int.TryParse(strport, out var port))
            {
                switch (dbType)
                {
                    case "mysql":
                        port = 3306;
                        break;
                    case "sqlserver":
                        port = 1433;
                        break;
                    default:
                        return null;
                }
            }
            var uid = atts["Uid"]?.Value;
            var pwd = atts["Pwd"]?.Value;
            var db = atts["Database"]?.Value;
            var exportData = atts["BackData"]?.Value;
            if (string.IsNullOrEmpty(server) || string.IsNullOrEmpty(uid) || string.IsNullOrEmpty(pwd))
            {
                return null;
            }
            var item = new DbItem
            {
                Type = EnumHelper.ToEnum(dbType, DbType.MySql),
                Server = server,
                Port = port,
                Uid = uid,
                Pwd = pwd,
                Database = db,
                ExportData = !string.IsNullOrWhiteSpace(exportData) &&
                             exportData.Equals("true", StringComparison.OrdinalIgnoreCase),
            };
            return item;
        }
    }
}

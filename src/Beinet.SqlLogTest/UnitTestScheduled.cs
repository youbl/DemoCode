using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Beinet.Core.Cron;
using Beinet.Core.Reflection;
using Beinet.SqlLog;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MySql.Data.MySqlClient;
using NLog;

namespace Beinet.SqlLogTest
{
    [TestClass]
    public class UnitTestScheduled
    {
        /// <summary>
        /// 
        /// </summary>
        [TestMethod]
        public void Testxxx()
        {
            List<UnitTestScheduled> arr = null;
            if (arr is List<UnitTestScheduled>)
            {
                Console.WriteLine(111);
            }
        }
    }
}

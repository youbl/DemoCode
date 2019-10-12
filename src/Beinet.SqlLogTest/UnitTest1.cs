using System;
using System.Data.SqlClient;
using Beinet.SqlLog;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MySql.Data.MySqlClient;

namespace Beinet.SqlLogTest
{
    [TestClass]
    public class UnitTest1
    {
        /// <summary>
        /// 执行完方法，在运行目录下的logs下，应该会有对应的SQL Server语句
        /// </summary>
        [TestMethod]
        public void TestSqlServerLog()
        {
            SqlExecutePatcher.Patch();

            object ret;
            string val = "1578";
            using (var connection = new SqlConnection("server=10.2.5.2;database=master;uid=sa;pwd=mike.123"))
            using (var command = connection.CreateCommand())
            {
                connection.Open();
                command.CommandText = "select " + val;
                ret = command.ExecuteScalar();
            }

            Assert.AreEqual(ret.ToString(), val);
        }

        /// <summary>
        /// 执行完方法，在运行目录下的logs下，应该会有对应的MySQL语句
        /// </summary>
        [TestMethod]
        public void TestMySqlLog()
        {
            SqlExecutePatcher.Patch();

            object ret;
            var constr =
                "server=testmysql.chidaoni.net;Port=3306;Database=ConfigsCenter;uid=mike;pwd=mike.123;Pooling=True;Max Pool Size=10;Charset=utf8";
            string val = "1578";
            using (var connection = new MySqlConnection(constr))
            using (var command = connection.CreateCommand())
            {
                connection.Open();
                command.CommandText = "select " + val + " from dual";
                ret = command.ExecuteScalar();
            }

            Assert.AreEqual(ret.ToString(), val);
        }
    }
}

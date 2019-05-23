using System;
using Beinet.Core;
using Beinet.Core.Database;

namespace DemoCodeConsole.CoreTest
{
    class LoggingTest : IRunable
    {
        public void Run()
        {
            var sqlHelper = BaseSqlHelper.GetConnection<SqlHelper>("server=127.0.0.1;database=db1;uid=sa;pwd=123");
            var sql = "insert into tb(id,name,content,ttype)values(@a,@b,@c,@d)";
            sqlHelper.ExecuteNonQuery(sql,
                sqlHelper.CreatePara("@a", "11"),
                sqlHelper.CreatePara("@b", "11"),
                sqlHelper.CreatePara("@c", "11"),
                sqlHelper.CreatePara("@d", "11")
            );


            throw new NotImplementedException();
        }
    }
}

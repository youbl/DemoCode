using System;
using Beinet.Repository;
using Beinet.RepositoryTest.Repository;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Beinet.RepositoryTest
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            var repository = ProxyLoader.GetProxy<RepositoryStudent>();
            var cnt = repository.Count();
            Assert.AreEqual(1, cnt);
        }
    }
}

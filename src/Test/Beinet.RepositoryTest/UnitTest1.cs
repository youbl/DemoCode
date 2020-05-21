using System;
using System.Collections.Generic;
using System.Threading;
using Beinet.Repository;
using Beinet.RepositoryTest.Entitys;
using Beinet.RepositoryTest.Repository;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Beinet.RepositoryTest
{
    [TestClass]
    public class UnitTest1
    {
        /// <summary>
        /// 启动测试前，表的数据总行数。
        /// 必须大于20，下面的测试才能正常运行
        /// </summary>
        private const int TABLE_ROWNUM = 52;

        RepositoryStudent repository = ProxyLoader.GetProxy<RepositoryStudent>();

        /// <summary>
        /// JPA的默认DQL方法测试
        /// </summary>
        [TestMethod]
        public void BaseSelectMethodTest()
        {
            var cnt = repository.Count();
            Assert.AreEqual(TABLE_ROWNUM, cnt);

            var arr = repository.FindAll();
            Assert.AreEqual(arr.Count, cnt);

            var item = repository.FindById(arr[2].Id);
            Assert.AreEqual(item?.ToString(), arr[2].ToString());

            var arrIds = new List<long>();
            for (var i = 1; i < 20; i += 2)
            {
                arrIds.Add(arr[i].Id);
            }

            var arrByIds = repository.FindAllById(arrIds);
            Assert.AreEqual(10, arrByIds.Count);
            Assert.AreEqual(arrByIds[9].ToString(), arr[19].ToString());

            var exists = repository.ExistsById(arr[arr.Count - 1].Id);
            Assert.AreEqual(true, exists);
            exists = repository.ExistsById(12345678901234);
            Assert.AreEqual(false, exists);
            
        }

        /// <summary>
        /// JPA的默认DML方法测试
        /// </summary>
        [TestMethod]
        public void BaseDmlMethodTest()
        {
            var entity = new Student
            {
                AreaName = "福建省",
                MaxPeople = 1234567,
                MinPeople = 321,
                RestId = 98765,
                Sort = 888
            };
            // INSERT
            var savedEntity = repository.Save(entity);
            Assert.AreNotEqual(0, savedEntity.Id);
            entity.Id = savedEntity.Id;
            entity.CreationTime = savedEntity.CreationTime;
            entity.LastTime = savedEntity.LastTime;
            Assert.AreEqual(entity.ToString(), savedEntity.ToString());

            Thread.Sleep(1000); 

            // UPDATE
            savedEntity.AreaName = "福建省福州市";
            savedEntity = repository.Save(savedEntity);
            savedEntity.CreationTime = DateTime.Now.AddDays(-11); // 不会更新
            savedEntity.LastTime = DateTime.Now.AddDays(-12); // 不会更新
            Assert.AreEqual(entity.Id, savedEntity.Id);
            var updatedEntity = repository.FindById(savedEntity.Id);
            Assert.AreEqual("福建省福州市", updatedEntity.AreaName);
            Assert.AreEqual(entity.CreationTime, updatedEntity.CreationTime);
            Assert.AreNotEqual(entity.LastTime, updatedEntity.LastTime);

            var cnt = repository.Count();
            Assert.AreEqual(TABLE_ROWNUM, cnt - 1);

//            var affected = repository.Delete(savedEntity);
//            Assert.AreEqual(1, affected);

            entity.AreaName = "福建省福州市鼓楼区";
            var arrEntitys = new Student[]
            {
                new Student
                {
                    AreaName = "广州",
                }, 
                entity,
                new Student
                {
                    AreaName = "北京",
                    Sort = 444,
                },
                new Student
                {
                    AreaName = "上海",
                    MinPeople = 555
                },
            };

            var savedArr = repository.SaveAll(arrEntitys);
            Assert.AreNotEqual(0, savedArr[0].Id);
            Assert.AreNotEqual(0, savedArr[2].Id);
            Assert.AreNotEqual(0, savedArr[3].Id);

            var updatedEntity2 = repository.FindById(entity.Id);
            Assert.AreEqual(updatedEntity2.AreaName, savedArr[1].AreaName);
            Assert.AreEqual("福建省福州市鼓楼区", savedArr[1].AreaName);

            var delCnt = repository.DeleteAll(savedArr);
            Assert.AreEqual(4, delCnt);

            cnt = repository.Count();
            Assert.AreEqual(TABLE_ROWNUM, cnt);
        }

    }
}
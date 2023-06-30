using System.Collections.Generic;
using Beinet.Repository;
using Beinet.Repository.Repositories;
using Beinet.RepositoryTest.Entitys;

namespace Beinet.RepositoryTest.Repository
{
    /// <summary>
    /// 数据库持久层
    /// </summary>
    [DataSourceConfiguration("{dbconn}")]
    public interface RepositoryStudent : JpaRepository<Student, long>
    {
        [Query("SELECT COUNT(1) FROM #{#entityName}")]
        int DoCount();

        [Query("SELECT * from #{#entityName} where minpeople>?1 and maxpeople<?2 order by id desc")]
        Student SelectFirst(int min, int max);

        [Query("SELECT * from #{#entityName} where minpeople>?1 and maxpeople<?2 order by id desc")]
        Student[] SelectAll(int min, int max);
    }
}

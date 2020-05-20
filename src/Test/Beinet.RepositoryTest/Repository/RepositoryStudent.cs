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
        [Query("SELECT COUNT(1) FROM aaa WHERE 1=2")]
        int aaa();
    }
}

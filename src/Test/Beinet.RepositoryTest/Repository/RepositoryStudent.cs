using Beinet.Repository;
using Beinet.Repository.Repositories;
using Beinet.RepositoryTest.Entitys;

namespace Beinet.RepositoryTest.Repository
{
    /// <summary>
    /// 数据库持久层
    /// </summary>
    public interface RepositoryStudent : JpaRepository<Student, long>
    {
    }
}

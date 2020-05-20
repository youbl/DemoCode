
using Beinet.Repository.Entitys;

namespace Beinet.Repository.Repositories
{
    /// <summary>
    /// JPA操作接口
    /// </summary>
    interface RepositoryProperty
    {
        /// <summary>
        /// 数据库连接字符串
        /// </summary>
        DataSourceConfigurationAttribute DataSource { get; set; }
        /// <summary>
        /// T实体类的信息
        /// </summary>
        EntityData Data { get; set; }

    }
}

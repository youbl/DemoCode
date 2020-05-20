using System.Collections.Generic;

namespace Beinet.Repository.Repositories
{
    /// <summary>
    /// JPA操作接口
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    /// <typeparam name="ID">主键类型</typeparam>
    public interface JpaRepository<T, ID>// where T : new()
    {
        /// <summary>
        /// 返回所有记录
        /// </summary>
        /// <returns></returns>
        List<T> FindAll();

        // Page<T> findAll(Pageable pageable);

        /// <summary>
        /// 根据主键列表返回数据
        /// </summary>
        /// <param name="arrIds"></param>
        /// <returns></returns>
        List<T> FindAllById(IEnumerable<ID> arrIds);

        /// <summary>
        /// 返回记录总数
        /// </summary>
        /// <returns></returns>
        long Count();

        /// <summary>
        /// 根据主键删除
        /// </summary>
        /// <param name="aID">主键</param>
        int DeleteById(ID aID);

        /// <summary>
        /// 根据实体删除
        /// </summary>
        /// <param name="entity">实体</param>
        int Delete(T entity);

        /// <summary>
        /// 根据实体列表删除
        /// </summary>
        /// <param name="arrEntities">实体列表</param>
        int DeleteAll(IEnumerable<T> arrEntities);

        /// <summary>
        /// 根据主键列表删除
        /// </summary>
        /// <param name="arrIds">主键列表</param>
        int DeleteAll(IEnumerable<ID> arrIds);

        /// <summary>
        /// 保存实体
        /// </summary>
        /// <param name="entity">实体</param>
        /// <returns></returns>
        T Save(T entity);

        /// <summary>
        /// 批量保存实体
        /// </summary>
        /// <param name="arrEntities"></param>
        /// <returns></returns>
        List<T> SaveAll(IEnumerable<T> arrEntities);

        /// <summary>
        /// 根据主键查找
        /// </summary>
        /// <param name="aID">主键</param>
        /// <returns></returns>
        T FindById(ID aID);

        /// <summary>
        /// 指定主键是否存在
        /// </summary>
        /// <param name="aID">主键</param>
        /// <returns></returns>
        bool ExistsById(ID aID);

        // void flush();
        // T saveAndFlush(T s);


        //void deleteInBatch(IEnumerable<T> IEnumerable);

        //void deleteAllInBatch();

        //T getOne(ID aID);
    }
}

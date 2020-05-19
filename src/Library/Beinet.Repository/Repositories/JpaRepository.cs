using System.Collections.Generic;

namespace Beinet.Repository.Repositories
{
    /// <summary>
    /// JPA操作接口
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    /// <typeparam name="ID">主键类型</typeparam>
    public interface JpaRepository<T, ID>
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
        /// <param name="IEnumerable"></param>
        /// <returns></returns>
        List<T> FindAllById(IEnumerable<ID> IEnumerable);

        /// <summary>
        /// 返回记录总数
        /// </summary>
        /// <returns></returns>
        long Count();

        /// <summary>
        /// 根据主键删除
        /// </summary>
        /// <param name="aID">主键</param>
        void DeleteById(ID aID);

        /// <summary>
        /// 根据实体删除
        /// </summary>
        /// <param name="T">实体</param>
        void Delete(T T);

        /// <summary>
        /// 根据实体列表删除
        /// </summary>
        /// <param name="arr">实体列表</param>
        void DeleteAll(IEnumerable<T> arr);

        /// <summary>
        /// 根据主键列表删除
        /// </summary>
        /// <param name="arr">主键列表</param>
        void DeleteAll(IEnumerable<ID> arr);

        /// <summary>
        /// 保存实体
        /// </summary>
        /// <param name="s">实体</param>
        /// <returns></returns>
        T Save(T s);

        /// <summary>
        /// 批量保存实体
        /// </summary>
        /// <param name="IEnumerable"></param>
        /// <returns></returns>
        List<T> SaveAll(IEnumerable<T> IEnumerable);

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

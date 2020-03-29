using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Beinet.Repository
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
        List<T> findAll();

        // Page<T> findAll(Pageable pageable);

        /// <summary>
        /// 根据主键列表返回数据
        /// </summary>
        /// <param name="IEnumerable"></param>
        /// <returns></returns>
        List<T> findAllById(IEnumerable<ID> IEnumerable);

        /// <summary>
        /// 返回记录总数
        /// </summary>
        /// <returns></returns>
        long count();

        /// <summary>
        /// 根据主键删除
        /// </summary>
        /// <param name="aID">主键</param>
        void deleteById(ID aID);

        /// <summary>
        /// 根据实体删除
        /// </summary>
        /// <param name="T">实体</param>
        void delete(T T);

        /// <summary>
        /// 根据实体列表删除
        /// </summary>
        /// <param name="arr">实体列表</param>
        void deleteAll(IEnumerable<T> arr);

        /// <summary>
        /// 根据主键列表删除
        /// </summary>
        /// <param name="arr">主键列表</param>
        void deleteAll(IEnumerable<ID> arr);

        /// <summary>
        /// 保存实体
        /// </summary>
        /// <param name="s">实体</param>
        /// <returns></returns>
        T save(T s);

        /// <summary>
        /// 批量保存实体
        /// </summary>
        /// <param name="IEnumerable"></param>
        /// <returns></returns>
        List<T> saveAll(IEnumerable<T> IEnumerable);

        /// <summary>
        /// 根据主键查找
        /// </summary>
        /// <param name="aID">主键</param>
        /// <returns></returns>
        T findById(ID aID);

        /// <summary>
        /// 指定主键是否存在
        /// </summary>
        /// <param name="aID">主键</param>
        /// <returns></returns>
        bool existsById(ID aID);

        // void flush();
        // T saveAndFlush(T s);


        //void deleteInBatch(IEnumerable<T> IEnumerable);

        //void deleteAllInBatch();

        //T getOne(ID aID);
    }
}

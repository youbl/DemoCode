
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Beinet.Repository.Entitys;

namespace Beinet.Repository.Tools
{
    class SqlParser
    {
        /// <summary>
        /// 不同类型的处理委托程序
        /// </summary>
        /// <returns></returns>
        delegate string ParseDelegate(string whereSql, EntityData data);

        /// <summary>
        /// 不同的方法名，要使用的解析方法
        /// </summary>
        private static Dictionary<string, ParseDelegate> ArrPrefix { get; } = new Dictionary<string, ParseDelegate>
        {
            /* int countByDishHourAndRestId(int hour, long restId);
             * int countaaaByDishHourAndRestId(int hour, long restId);
             * int countaaasByDishHourAndRestId(int hour, long restId);
             * int countAllByDishHourAndRestId(int hour, long restId);
             * 对应SQL：
             * select count(id) from aaa where dishHour=? and restId=?
             * 注： countDistinctBy 没用，SQL：select distinct count(distinct id) 有啥意义，除非没主键……
             */
            {"^count[a-zA-Z]*?By(?=[A-Z])", ParseCount},

            /* boolean existsByDishHourAndRestId(int hour, long restId);
             * boolean existsaaaByDishHourAndRestId(int hour, long restId);
             * boolean existsaaasByDishHourAndRestId(int hour, long restId);
             * boolean existsAllByDishHourAndRestId(int hour, long restId);
             * s对应SQL：
             * select id from aaa where dishHour=? and restId=? limit 1
             * 注： existsDistinctBy 没用，SQL：select distinct id 有啥意义，有没有记录都是一样的……
             */
            {"^exists[a-zA-Z]*?By(?=[A-Z])", ParseExists},
            
            /* @Transactional
             * int deleteaaaByDishHourAndRestId(int a, long b);
             * @Transactional
             * int deleteaaasByDishHourAndRestId(int a, long b);
             * @Transactional
             * int deleteAllByDishHourAndRestId(int a, long b);
             * @Transactional
             * int deleteByDishHourAndRestId(int a, long b);
             * 
             * @Transactional
             * int removeaaaByDishHourAndRestId(int a, long b);
             * @Transactional
             * int removeaaasByDishHourAndRestId(int a, long b);
             * @Transactional
             * int removeAllByDishHourAndRestId(int a, long b);
             * @Transactional
             * int removeByDishHourAndRestId(int a, long b);
             * 
             * 之所以要求事务，是因为 Hibernate 用2条SQL查询2遍:
             * select id,dishHour,num,restId from aaa where dishHour=? and restId=?
             * delete from aaa where id=?
             * 注： deleteDistinctBy 没用，SQL：select distinct id 有啥意义，除非没主键……
             */
            {"^delete[a-zA-Z]*?By(?=[A-Z])", ParseDelete},
            {"^remove[a-zA-Z]*?By(?=[A-Z])", ParseDelete},
            
            // 注意这个findFirstBy 要在 findBy前面，不然会匹配错误的Parse方法
            /* aaa findFirstByDishHourAndRestId(int hour, long restId);
             * aaa findTopByDishHourAndRestId(int a, long b);
             * 
             * aaa getFirstByDishHourAndRestId(int hour, long restId);
             * aaa getTopByDishHourAndRestId(int a, long b);
             * 
             * aaa queryFirstByDishHourAndRestId(int hour, long restId);
             * aaa queryTopByDishHourAndRestId(int a, long b);
             * 
             * aaa readFirstByDishHourAndRestId(int hour, long restId);
             * aaa readTopByDishHourAndRestId(int a, long b);
             * 
             * aaa streamFirstByDishHourAndRestId(int hour, long restId);
             * aaa streamTopByDishHourAndRestId(int a, long b);
             * 对应SQL:
             * select id,dishHour,num,restId from aaa where dishHour=? and restId=? limit 1
             * 注1：返回值也可以是 List<aaa>，但是只会返回第一行
             * 注2：findDistinctFirstBy 没用，SQL：select distinct id 有啥意义，除非没主键……
             */
            {"^findFirstBy", ParseFindFirst},
            {"^findTopBy", ParseFindFirst},
            {"^getFirstBy", ParseFindFirst},
            {"^getTopBy", ParseFindFirst},
            {"^queryFirstBy", ParseFindFirst},
            {"^queryTopBy", ParseFindFirst},
            {"^readFirstBy", ParseFindFirst},
            {"^readTopBy", ParseFindFirst},
            {"^streamFirstBy", ParseFindFirst},
            {"^streamTopBy", ParseFindFirst},

            /* List<aaa> findByDishHourAndRestId(int hour, long restId);
             * List<aaa> findaaaByDishHourAndRestId(int hour, long restId);
             * List<aaa> findaaasByDishHourAndRestId(int hour, long restId);
             * List<aaa> findAllByDishHourAndRestId(int hour, long restId);
             * 
             * List<aaa> getByDishHourAndRestId(int hour, long restId);
             * List<aaa> getaaaByDishHourAndRestId(int hour, long restId);
             * List<aaa> getaaasByDishHourAndRestId(int hour, long restId);
             * List<aaa> getAllByDishHourAndRestId(int hour, long restId);
             * 
             * List<aaa> queryByDishHourAndRestId(int hour, long restId);
             * List<aaa> queryaaaByDishHourAndRestId(int hour, long restId);
             * List<aaa> queryaaasByDishHourAndRestId(int hour, long restId);
             * List<aaa> queryAllByDishHourAndRestId(int hour, long restId);
             * 
             * List<aaa> readByDishHourAndRestId(int hour, long restId);
             * List<aaa> readaaaByDishHourAndRestId(int hour, long restId);
             * List<aaa> readaaasByDishHourAndRestId(int hour, long restId);
             * List<aaa> readAllByDishHourAndRestId(int hour, long restId);
             * 
             * List<aaa> streamByDishHourAndRestId(int hour, long restId);
             * List<aaa> streamaaaByDishHourAndRestId(int hour, long restId);
             * List<aaa> streamaaasByDishHourAndRestId(int hour, long restId);
             * List<aaa> streamAllByDishHourAndRestId(int hour, long restId);
             * 对应SQL:
             * select id,dishHour,num,restId from aaa where dishHour=? and restId=?
             * 注： findDistinctBy 没用，SQL：select distinct id 有啥意义，除非没主键……
             */
            {"^find[a-zA-Z]*?By(?=[A-Z])", ParseFind},
            {"^get[a-zA-Z]*?By(?=[A-Z])", ParseFind},
            {"^query[a-zA-Z]*?By(?=[A-Z])", ParseFind},
            {"^read[a-zA-Z]*?By(?=[A-Z])", ParseFind},
            {"^stream[a-zA-Z]*?By(?=[A-Z])", ParseFind},

        };

        private static List<string> Keywords = new List<string>
        {
            "And",
            "Or",
            "Is",
            "Equals",
            "Between",
            "LessThan",
            "LessThanEqual",
            "GreaterThan",
            "GreaterThanEqual",
            "After",
            "Before",
            "IsNull",
            "Null",
            "IsNotNull",
            "NotNull",
            "Like",
            "NotLike",
            "StartingWith",
            "EndingWith",
            "Containing",
            "Not",
            "In",
            "NotIn",
            "TRUE",
            "FALSE",
            "IgnoreCase",
            "OrderBy",
        };

        private static string RegKeywords = "(" + Keywords.Aggregate("", (s, s1) => s + (s.Length > 0 ? "|" : "") + s1) + ")((?=[A-Z])|$)";

        /// <summary>
        /// 根据方法名，拆解出SQL,
        /// 参考JPA文档: https://docs.spring.io/spring-data/jpa/docs/current/reference/html/#reference
        /// </summary>
        /// <param name="name">方法名</param>
        /// <param name="arrPara">方法的参数列表</param>
        /// <param name="data">实体属性与字段映射</param>
        /// <returns></returns>
        public string GetSql(string name, ParameterInfo[] arrPara, EntityData data)
        {
            ParseDelegate parseDele = null;
            foreach (var itemDelegate in ArrPrefix)
            {
                var match = Regex.Match(name, itemDelegate.Key);
                if (match.Success)
                {
                    parseDele = itemDelegate.Value;
                    // 替换掉前缀
                    name = name.Substring(match.Value.Length);
                    break;
                }
            }
            if(parseDele == null || name.Length <= 0)
                throw new ArgumentException("方法名不符合规范：" + name);

            var whereSql = ParseWhere(name);
            return parseDele(whereSql, data);
        }

        private static string ParseWhere(string name)
        {
            var ret = new StringBuilder();
            var match = Regex.Match(name, RegKeywords);
            while (match.Success)
            {

                match = match.NextMatch();
            }

            return ret.ToString();
        }

        private static string ParseCount(string whereSql, EntityData data)
        {
            return $"SELECT COUNT(1) FROM {data.TableName} {whereSql}";
        }
        private static string ParseExists(string whereSql, EntityData data)
        {
            return $"SELECT 1 FROM {data.TableName} {whereSql} LIMIT 1";
        }
        private static string ParseDelete(string whereSql, EntityData data)
        {
            return $"DELETE FROM {data.TableName} {whereSql}";
        }
        private static string ParseFindFirst(string whereSql, EntityData data)
        {
            return $"{data.SelectSql} {whereSql} LIMIT 1";
        }
        private static string ParseFind(string whereSql, EntityData data)
        {
            return $"{data.SelectSql} {whereSql}";
        }
    }
}

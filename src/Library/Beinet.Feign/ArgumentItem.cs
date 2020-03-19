using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using LinFu.DynamicProxy;

namespace Beinet.Feign
{
    /// <summary>
    /// 参数项类
    /// </summary>
    public class ArgumentItem 
    {
        /// <summary>
        /// 参数名称
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// http传输时使用的名称
        /// </summary>
        public string HttpName { get; set; }
        /// <summary>
        /// 参数的值
        /// </summary>
        public object Value { get; set; }

        /// <summary>
        /// 发送类型
        /// </summary>
        public ArgType Type { get; set; }

    }
   
}
﻿using System;

namespace Beinet.Repository.Entitys
{
    /// <summary>
    /// 表示非数据库定义的字段
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class TransientAttribute : Attribute
    {
    }
}

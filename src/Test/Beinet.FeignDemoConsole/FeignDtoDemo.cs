using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Beinet.FeignDemoConsole
{
    /// <summary>
    /// DTO对象，属性可以跟响应的大小写 不一样
    /// </summary>
    public class FeignDtoDemo
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime AddTime { get; set; }
        public Work[] Works { get; set; }

        public string Url { get; set; }
        public string Stream { get; set; }
        public string Post { get; set; }
        public Dictionary<string, string> Headers { get; set; }

        public override string ToString()
        {
            var ret = new StringBuilder();
            foreach (var propertyInfo in this.GetType().GetProperties())
            {
                ret.AppendFormat(", {0}: ", propertyInfo.Name);
                var val = propertyInfo.GetValue(this, null);
                if (!(val is string) && val is IEnumerable arr)
                {
                    ret.Append("[ ");
                    foreach (var item in arr)
                    {
                        ret.AppendFormat("[{0}], ", item ?? "null");
                    }

                    ret.Remove(ret.Length - 2, 2);
                    ret.Append(" ]");
                }
                else
                    ret.AppendFormat("{0}", val);
            }
            ret.Remove(0, 2);
            ret.Insert(0, this.GetType().FullName + ": ");
            return ret.ToString();
        }
    }

    public class Work
    {
        public int Id { get; set; }
        public string Company { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }

        public override string ToString()
        {
            var ret = new StringBuilder();
            foreach (var propertyInfo in this.GetType().GetProperties())
            {
                ret.AppendFormat(", {0}: ", propertyInfo.Name);
                var val = propertyInfo.GetValue(this, null);
                if (!(val is string) && val is IEnumerable arr)
                {
                    ret.Append("[ ");
                    foreach (var item in arr)
                    {
                        ret.AppendFormat("[{0}], ", item ?? "null");
                    }

                    ret.Remove(ret.Length - 2, 2);
                    ret.Append(" ]");
                }
                else
                    ret.AppendFormat("{0}", val);
            }
            ret.Remove(0, 2);
            ret.Insert(0, this.GetType().FullName + ": ");
            return ret.ToString();
        }
    }
}

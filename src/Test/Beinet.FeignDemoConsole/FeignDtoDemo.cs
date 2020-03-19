using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace Beinet.FeignDemoConsole
{
    // DTO对象，属性可以跟响应的大小写 不一样
    public class FeignDtoDemo
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime AddTime { get; set; }
        public Work[] Works { get; set; }

        public string Url { get; set; }  // api支持，调用的完整url
        public string Post { get; set; } // api支持，调用的完整Form数据，比如a=1&b=2
        public string Stream { get; set; }// api支持，调用的完整Stream流数据，比如json
        public Dictionary<string, string> Headers { get; set; }// api支持，请求的完整Header
    }

    public class Work
    {
        public int Id { get; set; }
        public string Company { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
    }
}
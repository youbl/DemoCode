using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace Beinet.Core.Xml
{
    /// <summary>
    /// XML相关辅助方法
    /// </summary>
    public static class XmlHelper
    {
        /// <summary>
        /// 从XmlNode节点转化为对象的委托
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="node">XML节点，限定为 XmlNodeType.Element</param>
        /// <returns>对象实例或null</returns>
        public delegate T CreateItem<out T>(XmlNode node);

        /// <summary>
        /// 加载XML文件，返回解析对象列表
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="filename">XML文件</param>
        /// <param name="method">把XML节点转换为对象的委托</param>
        /// <returns>对象列表</returns>
        public static List<T> GetListFromFile<T>(string filename, CreateItem<T> method)
        {
            if (string.IsNullOrEmpty(filename) || !File.Exists(filename))
                return new List<T>();

            return GetList(File.ReadAllText(filename), method);
        }

        /// <summary>
        /// 加载XML内容，返回解析对象列表
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="content">XML内容</param>
        /// <param name="method">把XML节点转换为对象的委托</param>
        /// <returns>对象列表</returns>
        public static List<T> GetList<T>(string content, CreateItem<T> method)
        {
            if (string.IsNullOrEmpty(content))
                return new List<T>();

            var xml = new XmlDocument();
            xml.LoadXml(content);
            var root = xml.DocumentElement;
            if (root == null)
                return new List<T>();

            var ret = new List<T>();
            foreach (XmlNode configNode in root.ChildNodes)
            {
                if (configNode.NodeType != XmlNodeType.Element)
                {
                    continue;
                }
                var item = method(configNode);
                if (item != null)
                {
                    ret.Add(item);
                }
            }

            return ret;
        }
    }
}

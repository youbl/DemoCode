using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Web.Routing;

namespace Beinet.EnumList
{
    /// <summary>
    /// 遍历所有类库，并列出所有的枚举，进行展示.
    /// 在 /Properties/AssemblyInfo.cs 里调用：
    /// [assembly: PreApplicationStartMethod(typeof(EnumSearcher), "Init")]
    /// </summary>
    public class EnumSearcher
    {
        /// <summary>
        /// 收集到的所有枚举, key为枚举类名，value为枚举项的数值和字符串定义
        /// </summary>
        public static Dictionary<string, EnumClassRecord> ArrEnums { get; } = new Dictionary<string, EnumClassRecord>();

        /// <summary>
        /// 要扫描枚举的目录，默认为程序目录下的bin子目录
        /// </summary>
        public static string Dir { get; set; } = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "bin");
        // private static readonly ILogger _logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// 在AssemblyInfo.cs里定义的初始化方法
        /// </summary>
        public static void Init()
        {
            try
            {
                var route = "actuator/enums";
                RouteTable.Routes.Add(new Route(route, new EnumHttpHandler()));
                // _logger.Info($"路由注册成功：{route}, 开始扫描dll");

                ScanAllEnums(Dir);
            }
            // ReSharper disable once EmptyGeneralCatchClause
            catch
            {

            }
        }

        static void ScanAllEnums(string dir)
        {
            // 需要扫描的dll前缀
            string scanAssemblyRegex = "(^(?i)beinet)|UnitTest";
            var arrAssembly = ScanAssembly(dir, scanAssemblyRegex);

            //  当从非托管代码调用时可返回 null
            var entry = Assembly.GetEntryAssembly();
            if (entry != null)
                arrAssembly.Add(entry);

            // _logger.Info($"dll加载个数：{arrAssembly.Count.ToString()}, 开始扫描枚举");

            foreach (var assembly in arrAssembly)
            {
                ScanPerAssembly(assembly);
            }
        }

        static void ScanPerAssembly(Assembly assembly)
        {
            try
            {
                foreach (var type in assembly.GetTypes())
                {
                    var typeEnums = ParseEnums(type);
                    if (typeEnums != null)
                    {
                        var typeName = type.FullName ?? "";
                        if (ArrEnums.ContainsKey(typeName))
                            continue;
                        ArrEnums.Add(typeName, typeEnums);
                    }
                }
            }
            catch
            {
                // GetTypes可能抛异常，忽略
            }
        }

        /// <summary>
        /// 扫描并加载指定目录下的组件返回。
        /// </summary>
        /// <param name="dir">目录名</param>
        /// <param name="regex">目录名要匹配的正则</param>
        /// <returns></returns>
        static HashSet<Assembly> ScanAssembly(string dir, string regex = null)
        {
            var arrAssembly = new HashSet<Assembly>();
            if (!Directory.Exists(dir))
                return arrAssembly;

            Regex regObj = null;
            if (!string.IsNullOrEmpty(regex))
                regObj = new Regex(regex);

            var files = Directory.GetFiles(dir, "*.dll", SearchOption.TopDirectoryOnly);
            foreach (var file in files)
            {
                var assemblyString = Path.GetFileNameWithoutExtension(file);
                if (string.IsNullOrEmpty(assemblyString) || (regObj != null && !regObj.IsMatch(assemblyString)))
                    continue;
                try
                {
                    // 尝试加载dll，不能加载文件，会锁住文件
                    var ass = Assembly.Load(assemblyString);
                    if (ass == null)
                    {
                        continue;
                    }

                    arrAssembly.Add(ass);
                }
                // ReSharper disable once EmptyGeneralCatchClause
                catch (Exception)
                {
                }
            }

            return arrAssembly;
        }

        static EnumClassRecord ParseEnums(Type enumType)
        {
            if (!enumType.IsEnum || string.IsNullOrEmpty(enumType.FullName))
                return null;

            var ret = new EnumClassRecord
            {
                Description = GetDescription(enumType)
            };

            // foreach (var field in enumType.GetFields())
            foreach (var val in Enum.GetValues(enumType))
            {
                var definition = Enum.GetName(enumType, val);
                if (string.IsNullOrEmpty(definition))
                    continue;

                // 不能用(int)val，因为也可能是byte
                var code = Convert.ToInt32(val);
                var description = GetDescription(enumType, definition);

                // 项目出现bug时，比如定义了2个相同数字的code，导致异常
                if (ret.Enums.ContainsKey(definition))
                {
                    continue;
                }

                ret.Enums.Add(definition, new EnumRecord {Value = code, Description = description});
            }

            return ret;
        }

        static string GetDescription(Type enumType, string field)
        {
            FieldInfo fieldInfo = enumType.GetField(field);
            if (fieldInfo == null)
                return string.Empty;

            foreach (var attribute in fieldInfo.GetCustomAttributes())
            {
                if (attribute == null)
                    continue;
                if (attribute is DescriptionAttribute descAtt)
                    return descAtt.Description;
                else if (attribute.ToString().IndexOf("LanguageTag", StringComparison.Ordinal) > 0)
                {
                    // 尝试读取 语言标签
                    var prop = attribute.GetType().GetProperty("Message");
                    if (prop == null)
                        continue;
                    return Convert.ToString(prop.GetValue(attribute));
                }
            }

            return null;
        }

        static string GetDescription(Type type)
        {
            var att = type.GetCustomAttribute<DescriptionAttribute>();
            return att?.Description;
        }
    }
}
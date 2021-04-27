using System;
using System.Data;
using System.Linq;
using System.Reflection;
using Harmony;
using NLog;

namespace Beinet.SqlLog
{
    public class SqlExecutePatcher
    {
        static ILogger _logger = LogManager.GetCurrentClassLogger();

        public static void Patch()
        {
            PatchSqlServer();
            PatchMySql();
        }


        /// <summary>
        /// 拦截SqlServer的SQL，
        /// SqlCommand底层都是通过 RunExecuteReader 方法执行
        /// </summary>
        static void PatchSqlServer()
        {
            var assemblyName = "System.Data";
            var typeName = "System.Data.SqlClient.SqlCommand";

            try
            {
                var assembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(type => type.GetName().Name == assemblyName);
                if (assembly == null)
                {
                    _logger.Warn($"{assemblyName} not exists, patch fail.");
                    return;
                }

                var para = new[]
                {
                    typeof(CommandBehavior),
                    assembly.GetType("System.Data.SqlClient.RunBehavior"),
                    typeof(bool),
                    typeof(string)
                };
                PatchCommand(assembly, typeName, "RunExecuteReader", para);
            }
            catch (Exception exp)
            {
                _logger.Error(nameof(PatchSqlServer) + " error: " + exp);
            }
        }

        static void PatchMySql()
        {
            var assemblyName = "MySql.Data";
            var typeName = "MySql.Data.MySqlClient.MySqlCommand";
            try
            {
                var assembly = Assembly.Load(assemblyName);
                if (assembly == null)
                {
                    _logger.Warn($"{assemblyName} not exists, patch fail.");
                    return;
                }

                PatchCommand(assembly, typeName, "ExecuteReader", new[] {typeof(CommandBehavior)});
            }
            catch (Exception exp)
            {
                _logger.Error(nameof(PatchMySql) + " error: " + exp);
            }
        }

        static void PatchCommand(Assembly assembly, string typeName, string methodName, Type[] para)
        {
            var commandType = assembly.GetType(typeName);
            if (commandType == null)
            {
                _logger.Warn($"{typeName} not found in {assembly}, patch fail.");
                return;
            }

            var flg = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            var executeReaderMethod = commandType.GetMethod(methodName, flg, null, para, null);
            if (executeReaderMethod == null)
            {
                _logger.Warn($"{methodName} not found in {typeName}, patch fail.");
                return;
            }

            var prefix = typeof(SqlFilterProcess).GetMethod("Prefix");
            var postfix = typeof(SqlFilterProcess).GetMethod("Postfix");

            DoPatch(executeReaderMethod, prefix, postfix);
            _logger.Info($"{typeName}.{methodName} was patched.");

        }

        /// <summary>
        /// 执行补丁，在指定方法前后增加方法处理
        /// </summary>
        /// <param name="original">要打补丁的原始方法</param>
        /// <param name="prefix">执行原始方法前要执行的方法</param>
        /// <param name="postfix">执行原始方法后要执行的方法</param>
        /// <param name="transpiler"></param>
        static void DoPatch(MethodBase original,
            MethodInfo prefix = null, MethodInfo postfix = null,
            MethodInfo transpiler = null)
        {
            var harmonyPrefix = prefix != null ? new HarmonyMethod(prefix) : null;
            var harmonySuffix = postfix != null ? new HarmonyMethod(postfix) : null;
            var harmonyTranspiler = transpiler != null ? new HarmonyMethod(transpiler) : null;

            var harmony = HarmonyInstance.Create("cn.beinet");
            harmony.Patch(original, harmonyPrefix, harmonySuffix, harmonyTranspiler);
        }
    }
}
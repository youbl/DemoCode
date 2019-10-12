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

                var commandType = assembly.GetType(typeName);
                if (commandType == null)
                {
                    _logger.Warn($"{typeName} not found in {assembly}, patch fail.");
                    return;
                }

                var para = new[]
                {
                    typeof(CommandBehavior),
                    assembly.GetType("System.Data.SqlClient.RunBehavior"),
                    typeof(bool),
                    typeof(string)
                };
                var executeReaderMethod = commandType.GetMethod("RunExecuteReader",
                    BindingFlags.Instance | BindingFlags.NonPublic, null,
                    para, null);
                if (executeReaderMethod == null)
                {
                    _logger.Warn($"RunExecuteReader not found in {typeName}, patch fail.");
                    return;
                }

                DoPatch(executeReaderMethod, typeof(SqlExecuteLog).GetMethod("Prefix"));
                _logger.Info($"{typeName}.RunExecuteReader was patched.");
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

                var commandType = assembly.GetType(typeName);
                if (commandType == null)
                {
                    _logger.Warn($"{typeName} not found in {assembly}, patch fail.");
                    return;
                }

                var executeReaderMethod = commandType.GetMethod("ExecuteReader",
                    BindingFlags.Instance | BindingFlags.Public, null,
                    new[] {typeof(CommandBehavior)}, null);
                if (executeReaderMethod == null)
                {
                    _logger.Warn($"ExecuteReader not found in {typeName}, patch fail.");
                    return;
                }

                DoPatch(executeReaderMethod, typeof(SqlExecuteLog).GetMethod("Prefix"));
                _logger.Info($"{typeName}.ExecuteReader was patched.");
            }
            catch (Exception exp)
            {
                _logger.Error(nameof(PatchMySql) + " error: " + exp);
            }
        }

        /// <summary>
        /// 执行补丁，在指定方法前后增加方法处理
        /// </summary>
        /// <param name="original"></param>
        /// <param name="prefix"></param>
        /// <param name="postfix"></param>
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

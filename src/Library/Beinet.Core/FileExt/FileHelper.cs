using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using NLog;

namespace Beinet.Core.FileExt
{
    /// <summary>
    /// 文件操作辅助类
    /// </summary>
    public static class FileHelper
    {
        private static ILogger log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// 无BOM头的UTF8格式，便于跟Java通讯
        /// </summary>
        public static Encoding UTF8_NoBom { get; } = new UTF8Encoding(false);

        public const string INVALID_CH = @"[\<\>\/\\\|\:""\*\?\r\n]";

        /// <summary>
        /// 字符串处理委托
        /// </summary>
        /// <param name="str"></param>
        public delegate void StringProcesser(string str);

        /// <summary>
        /// 指定的字符串是否合法Windows文件名
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static bool IsValidFileName(string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return false;
            }

            //  文件名中不允许出现的11个字符
            if (Regex.IsMatch(str, INVALID_CH))
            {
                return false;
            }

            // 不允许以这些文件名开头
            var deservedFileNames = new string[]
            {
                "CON.", "PRN.", "AUX.", "NUL.", "COM1.", "COM2.", "COM3.", "COM4.",
                "COM5.", "COM6.", "COM7.", "COM8.", "COM9.", "LPT1"
            };
            str = str.ToUpper();
            for (var i = deservedFileNames.Length - 1; i >= 0; i--)
            {
                if (str.IndexOf(deservedFileNames[i], StringComparison.Ordinal) == 0)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 替换：不可用于文件名的字符
        /// </summary>
        /// <param name="fileName">准备使用的文件名</param>
        /// <returns>可用的文件名</returns>
        public static string ReplaceInvalidChar(string fileName)
        {
            return Regex.Replace(fileName, INVALID_CH, "_");
        }


        /// <summary>
        /// 遍历指定文件的每一行，并进行处理，并返回处理行数
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="processMethod"></param>
        /// <param name="ignoreEmptyLine"></param>
        public static int EachLine(this string fileName, StringProcesser processMethod, bool ignoreEmptyLine = true)
        {
            if (fileName == null || (fileName = fileName.Trim()).Length <= 0)
                throw new ArgumentException("Filename can't be empty", nameof(fileName));

            if (!File.Exists(fileName))
                throw new FileNotFoundException("File not exists:", fileName);

            var ret = 0;
            using (var sr = new StreamReader(fileName, UTF8_NoBom))
            {
                while (!sr.EndOfStream)
                {
                    var line = (sr.ReadLine() ?? "").Trim();
                    if (ignoreEmptyLine && line.Length <= 0)
                        continue;
                    ret++;
                    processMethod(line);
                }
            }

            return ret;
        }

        /// <summary>
        /// 尝试删除文件, 失败会重试一次
        /// </summary>
        /// <param name="file">要删除的文件</param>
        /// <param name="retryWaitMillisecond">首次失败要等待的毫秒数</param>
        public static void TryDel(string file, int retryWaitMillisecond = 100)
        {
            if (!File.Exists(file))
                return;

            RetryHelper.Retry(() => File.Delete(file), 2, retryWaitMillisecond);
        }

        /// <summary>
        /// 先写入临时文件，再重命名的方案，避免写入时丢失内容
        /// </summary>
        /// <param name="targetFile">目标文件</param>
        /// <param name="content">文件内容</param>
        /// <param name="backup">目标存在时，是否备份</param>
        /// <param name="encoding">文件编码</param>
        public static void Save(string targetFile, string content, bool backup = true, Encoding encoding = null)
        {
            if (string.IsNullOrEmpty(targetFile))
            {
                throw new ArgumentNullException(nameof(targetFile), "filename can't be empty.");
            }

            if (content == null)
            {
                throw new ArgumentNullException(nameof(content), "content can't be null.");
            }

            if (encoding == null)
                encoding = UTF8_NoBom;

            {
                var dir = Path.GetDirectoryName(targetFile);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
            }

            // 临时文件精确到千万分之一毫秒，避免并发生成冲突，不考虑超过该时间的并发
            var tmpFile = targetFile + DateTime.Now.ToString("yyyyMMddHHmmssfffffff");
            TryDel(tmpFile);

            // 如下2种方案，都会写入操作系统缓存，不是直接写入磁盘，可能导致文件内容丢失
            // File.WriteAllText(queueInfoFile, queueInfoFile, encoding);
            // using (var sw = new StreamWriter(tmpFile, false, encoding))
            // {
            //     sw.Write(content);
            // }
            var bytes = encoding.GetBytes(content);
            using (var fs = new FileStream(tmpFile, FileMode.Create, FileAccess.ReadWrite, FileShare.None,
                       1024, FileOptions.WriteThrough))
            {
                fs.Write(bytes, 0, bytes.Length);
            }

            Move(tmpFile, targetFile, false, backup);
        }


        /// <summary>
        /// 复制源文件，到目标文件，失败会重试一次.
        /// 注：目标文件存在时会覆盖
        /// </summary>
        /// <param name="sourceFile">源文件</param>
        /// <param name="targetFile">目标文件</param>
        /// <param name="retryWaitMillisecond">首次失败要等待的毫秒数</param>
        public static void Copy(string sourceFile, string targetFile, int retryWaitMillisecond = 100)
        {
            Move(sourceFile, targetFile, true, false, retryWaitMillisecond);
        }

        /// <summary>
        /// 把源文件，移动到目标文件，失败会重试一次
        /// </summary>
        /// <param name="sourceFile">源文件</param>
        /// <param name="targetFile">目标文件</param>
        /// <param name="keepOld">源文件是否保留</param>
        /// <param name="backup">是否备份目标文件</param>
        /// <param name="retryWaitMillisecond">首次失败要等待的毫秒数</param>
        public static void Move(string sourceFile, string targetFile,
            bool keepOld = false, bool backup = false, int retryWaitMillisecond = 100)
        {
            var targetDir = Path.GetDirectoryName(targetFile);
            if (!string.IsNullOrEmpty(targetDir) && !Directory.Exists(targetDir))
                Directory.CreateDirectory(targetDir);

            RetryHelper.Retry(DoMove, 2, retryWaitMillisecond);

            void DoMove()
            {
                if (backup)
                {
                    if (File.Exists(targetFile))
                    {
                        var backFile = targetFile + DateTime.Now.ToString("yyyyMMddHHmmssfff");
                        // 先尝试删除旧文件，避免Move失败
                        TryDel(backFile);
                        // 备份旧文件
                        File.Move(targetFile, backFile);
                    }
                }
                else
                {
                    TryDel(targetFile);
                }

                if (keepOld)
                {
                    File.Copy(sourceFile, targetFile, true);
                }
                else
                {
                    File.Move(sourceFile, targetFile);
                }
            }
        }

        /// <summary>
        /// 把sourceDir下的文件和子目录，全部移动到targetDir下
        /// </summary>
        /// <param name="sourceDir"></param>
        /// <param name="targetDir"></param>
        public static void MoveDir(string sourceDir, string targetDir)
        {
            foreach (var file in Directory.GetFiles(sourceDir))
            {
                var targeFile = Path.Combine(targetDir, Path.GetFileName(file));
                Move(file, targeFile);
            }

            foreach (var dir in Directory.GetDirectories(sourceDir))
            {
                var targetSubDir = Path.Combine(targetDir, Path.GetFileName(dir));
                MoveDir(dir, targetSubDir); // 递归
            }

            try
            {
                Directory.Delete(sourceDir); // 移动完了，删除父目录
            }
            catch (Exception exp)
            {
                log.Error(exp, "{0}", sourceDir);
            }
        }

        /// <summary>
        /// 原生的Path.Combine，第2个参数不能以 斜杠开头，否则直接返回参数2
        /// </summary>
        /// <param name="dir"></param>
        /// <param name="filename"></param>
        /// <returns></returns>
        public static string PathCombine(string dir, string filename)
        {
            if (string.IsNullOrEmpty(dir))
                return filename;
            if (string.IsNullOrEmpty(filename))
                return dir;
            if (filename[0] == '/' || filename[0] == '\\')
            {
                filename = filename.Substring(1);
            }

            return Path.GetFullPath(Path.Combine(dir, filename));
        }
    }
}
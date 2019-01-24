using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace Beinet.Core.Util
{
    /// <summary>
    /// 
    /// </summary>
    public class FileHelper
    {
        /// <summary>
        /// 无BOM头的UTF8格式，便于跟Java通讯
        /// </summary>
        public static Encoding UTF8_NoBom { get; } = new UTF8Encoding(false);

        /// <summary>
        /// 读取文件内容返回
        /// </summary>
        /// <param name="targetFile"></param>
        /// <param name="encoding">文件编码格式，默认用UTF8读取</param>
        /// <returns></returns>
        public static string Read(string targetFile, Encoding encoding = null)
        {
            if(encoding == null)
                encoding = UTF8_NoBom;
            string content;
            using (var sr = new StreamReader(targetFile, encoding))
            {
                content = sr.ReadToEnd();
            }
            return content;
        }

        /// <summary>
        /// 把内容写入文件，如果文件存在，覆盖它
        /// </summary>
        /// <param name="targetFile"></param>
        /// <param name="content"></param>
        /// <param name="backup">如果文件存在，是否要备份</param>
        /// <param name="encoding">文件编码格式，默认用UTF8读取</param>
        /// <returns></returns>
        public static bool SaveToFile(string targetFile, string content, bool backup = true, Encoding encoding = null)
        {
            var tmpFile = InitAndSave(targetFile, content, encoding);
            // 迁移新文件
            Move(tmpFile, targetFile, false, backup);
            return true;
        }

        /// <summary>
        /// 保存到文件，并返回文件MD5
        /// </summary>
        /// <param name="targetFile"></param>
        /// <param name="content"></param>
        /// <param name="backup"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        public static string SaveAndGetMd5(string targetFile, string content, bool backup = true, Encoding encoding = null)
        {
            var tmpFile = InitAndSave(targetFile, content, encoding);
            string md5 = CryptoHelper.GetMD5HashFromFile(tmpFile);
            // 迁移新文件
            Move(tmpFile, targetFile, false, backup);
            return md5;
        }

        private static string InitAndSave(string targetFile, string content, Encoding encoding)
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
            // 临时文件精确到千万分之一毫秒，避免并发生成冲突
            var tmpFile = targetFile + DateTime.Now.ToString("yyyyMMddHHmmssfffffff");
            TryDel(tmpFile);
            
            // File.WriteAllText(queueInfoFile, queueInfoFile, encoding);
            using (var sw = new StreamWriter(tmpFile, false, encoding))
            {
                sw.Write(content);
            }
            return tmpFile;
        }

        /// <summary>
        /// 把源文件，移动到目标文件，会尝试移动两次
        /// </summary>
        /// <param name="sourceFile">源文件</param>
        /// <param name="targetFile">目标文件</param>
        /// <param name="keepOld">源文件是否保留</param>
        /// <param name="backup">是否备份目标文件</param>
        /// <param name="secondMoveWait">毫秒数,第一次移动失败，进行第二次移动前要等待的时间</param>
        /// <returns></returns>
        public static bool Move(string sourceFile, string targetFile,
            bool keepOld = false, bool backup = false, int secondMoveWait = 100)
        {
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

            try
            {
                DoMove();
            }
            catch (Exception)
            {
                Thread.Sleep(secondMoveWait);
                DoMove();
            }
            return true;
        }

        /// <summary>
        /// 尝试删除文件
        /// </summary>
        /// <param name="file"></param>
        /// <param name="secondMoveWait">毫秒数,第一次删除失败，进行第二次删除前要等待的时间</param>
        public static void TryDel(string file, int secondMoveWait = 100)
        {
            if (!File.Exists(file))
                return;
            try
            {
                File.Delete(file);
            }
            catch
            {
                Thread.Sleep(secondMoveWait);
                File.Delete(file);
            }
        }

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
            if (Regex.IsMatch(str, @"[\<\>\/\\\|\:""\*\?\r\n]"))
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
    }
}

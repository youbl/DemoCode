using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Beinet.Core.FileExt;
using Beinet.Core.Util;

namespace Beinet.Core.CryptExt
{
    /// <summary>
    /// 加密帮助类 
    /// </summary>
    public class CryptoHelper
    {
        private static readonly Encoding Utf8 = FileHelper.UTF8_NoBom;

        private static string _key;
        /// <summary>
        /// DES加解密的默认密钥和向量, 8位字符
        /// </summary>
        public static string Key
        {
            get
            {
                if (string.IsNullOrEmpty(_key))
                {
                    _key = ConfigHelper.GetSetting("_CryptKey");
                    if (string.IsNullOrEmpty(_key))
                        _key = "!$Beinet#";
                }
                return _key;
            }
        }

        private static string _des3_key;
        /// <summary>
        /// 3DES加解密的默认密钥, 24位字符, 前8位作为向量
        /// </summary>
        public static string DES3_KEY
        {
            get
            {
                if (string.IsNullOrEmpty(_des3_key))
                {
                    _des3_key = ConfigHelper.GetSetting("_Crypt3Key");
                    if (string.IsNullOrEmpty(_des3_key))
                        _des3_key = "Beinet.cn-K5&73#;0(=+)`!";
                }
                return _des3_key;
            }
        }

        private static string _aes_key;
        /// <summary>
        /// AES加解密的默认密钥, 32位字符
        /// </summary>
        public static string AES_KEY
        {
            get
            {
                if (string.IsNullOrEmpty(_aes_key))
                {
                    _aes_key = ConfigHelper.GetSetting("_CryptAesKey");
                    if (string.IsNullOrEmpty(_aes_key))
                        _aes_key = "Beinet.cnoiUUgG5%&73#;0(=+)`!.bn";
                }
                return _aes_key;
            }
        }

        #region  MD5加密

        /// <summary>
        /// 标准MD5加密
        /// </summary>
        /// <param name="source">待加密字符串</param>
        /// <param name="addKey">附加字符串</param>
        /// <param name="encoding">编码方式，为空时使用UTF-8</param>
        /// <returns></returns>
        public static string MD5(string source, string addKey = "", Encoding encoding = null)
        {
            if (addKey.Length > 0)
            {
                source = source + addKey;
            }
            if (encoding == null)
            {
                encoding = Utf8;
            }

            byte[] datSource = encoding.GetBytes(source);
            byte[] newSource;
            using (MD5 md5 = new MD5CryptoServiceProvider())
            {
                newSource = md5.ComputeHash(datSource);
            }
            string byte2String = BitConverter.ToString(newSource).Replace("-", "").ToLower();
            return byte2String;
        }
        
        /// <summary>
        /// 计算文件的MD5值并返回
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns>返回小写md5</returns>
        public static string GetMD5HashFromFile(string fileName)
        {
            using (var file = new FileStream(fileName, FileMode.Open))
            using (MD5 md5 = new MD5CryptoServiceProvider())
            {
                byte[] retVal = md5.ComputeHash(file);
                file.Close();
                return BitConverter.ToString(retVal).Replace("-", "").ToLower();
            }
        }
        #endregion
        
        
        #region  DES 加解密
        /// <summary>
        /// Desc加密
        /// </summary>
        /// <param name="source">待加密字符</param>
        /// <param name="key">密钥</param>
        /// <param name="encoding">编码格式，为空时使用UTF-8</param>
        /// <returns>string</returns>
        public static string DES_Encrypt(string source, string key = null, Encoding encoding = null)
        {
            if (string.IsNullOrEmpty(source))
                return null;
            if (string.IsNullOrEmpty(key))
                key = Key;
            if (encoding == null)
                encoding = Utf8;

            //把字符串放到byte数组中  
            byte[] inputByteArray = encoding.GetBytes(source);

            // 密钥必须是8位，否则会报错 System.ArgumentException: 指定键的大小对于此算法无效。
            var keyByte = GetKeyByte(key, 8, encoding);
            var ivByte = keyByte;

            byte[] result;
            using (DESCryptoServiceProvider des = new DESCryptoServiceProvider())
            {
                //建立加密对象的密钥和偏移量
                des.Key = keyByte;
                des.IV = ivByte;
                using (MemoryStream ms = new MemoryStream())
                using (ICryptoTransform form = des.CreateEncryptor())
                using (CryptoStream cs = new CryptoStream(ms, form, CryptoStreamMode.Write))
                {
                    cs.Write(inputByteArray, 0, inputByteArray.Length);
                    cs.FlushFinalBlock();
                    result = ms.ToArray();
                }
            }
            return Convert.ToBase64String(result);
            //return  BitConverter.ToString(newSource).Replace("-", "").ToUpper();
        }

        /// <summary>
        /// DES解密
        /// </summary>
        /// <param name="source">密文</param>
        /// <param name="key">密钥</param>
        /// <param name="encoding">编码格式，为空时使用UTF-8</param>
        /// <returns>明文</returns>
        public static string DES_Decrypt(string source, string key = null, Encoding encoding = null)
        {
            if (string.IsNullOrEmpty(source))
                return null;
            if (string.IsNullOrEmpty(key))
                key = Key;
            if (encoding == null)
                encoding = Utf8;

            //将字符串转为字节数组  
            byte[] inputByteArray = Convert.FromBase64String(source);
            // 密钥必须是8位，否则会报错 System.ArgumentException: 指定键的大小对于此算法无效。
            var keyByte = GetKeyByte(key, 8, encoding);
            var ivByte = keyByte;

            byte[] result;
            using (DESCryptoServiceProvider des = new DESCryptoServiceProvider())
            {
                //建立加密对象的密钥和偏移量，此值重要，不能修改  
                des.Key = keyByte;
                des.IV = ivByte;

                using (MemoryStream ms = new MemoryStream())
                using (ICryptoTransform form = des.CreateDecryptor())
                using (CryptoStream cs = new CryptoStream(ms, form, CryptoStreamMode.Write))
                {
                    cs.Write(inputByteArray, 0, inputByteArray.Length);
                    cs.FlushFinalBlock();
                    result = ms.ToArray();
                }
            }
            return encoding.GetString(result);
        }

        #endregion


        #region 3DES加解密
        /// <summary>
        /// 使用指定的key和iv，加密input数据
        /// </summary>
        /// <param name="input">待加密的字符串</param>
        /// <param name="encoding">编码格式，默认为UTF8</param>
        /// <param name="key">密钥，必须为24位长度</param>
        /// <param name="iv">向量，必须为8位长度</param>
        /// <returns></returns>
        public static string TripleDES_Encrypt(string input, Encoding encoding = null, string key = null, string iv = null)
        {
            if (encoding == null)
            {
                encoding = Utf8;
            }

            key = key ?? DES3_KEY;
            byte[] arrKey = GetKeyByte(key, 24, encoding);
            byte[] arrIV = GetKeyByte(key, 8, encoding);

            // 获取加密后的字节数据
            byte[] arrData = encoding.GetBytes(input);
            byte[] result = TripleDesEncrypt(arrKey, arrIV, arrData);

            // 转换为16进制字符串
            return Convert.ToBase64String(result);
            //return BitConverter.ToString(newSource).Replace("-", "").ToUpper();
        }


        /// <summary>
        /// 使用指定的key和iv，解密input数据
        /// </summary>
        /// <param name="input">待解密的字符串</param>
        /// <param name="encoding">编码格式，默认为UTF8</param>
        /// <param name="key">密钥，必须为24位长度</param>
        /// <param name="iv">向量，必须为8位长度</param>
        /// <returns></returns>
        public static string TripleDES_Decrypt(string input, Encoding encoding = null, string key = null, string iv = null)
        {
            if (encoding == null)
            {
                encoding = Utf8;
            }

            key = key ?? DES3_KEY;
            byte[] arrKey = GetKeyByte(key, 24, encoding);
            byte[] arrIV = GetKeyByte(key, 8, encoding);

            // 获取加密后的字节数据
            byte[] arrData = Convert.FromBase64String(input);
            byte[] result = TripleDesDecrypt(arrKey, arrIV, arrData);
            return encoding.GetString(result);
        }


        #region 私有方法：TripleDesEncrypt加密(3DES加密)
        /// <summary>
        /// 3Des加密，密钥长度必需是24字节
        /// </summary>
        /// <param name="key">密钥字节数组</param>
        /// <param name="iv">向量字节数组</param>
        /// <param name="source">源字节数组</param>
        /// <returns>加密后的字节数组</returns>
        private static byte[] TripleDesEncrypt(byte[] key, byte[] iv, byte[] source)
        {
            using (var dsp = new TripleDESCryptoServiceProvider())
            {
                dsp.Mode = CipherMode.CBC; // 默认值
                dsp.Padding = PaddingMode.PKCS7; // 默认值

                using (ICryptoTransform form = dsp.CreateEncryptor(key, iv))
                using (var mStream = new MemoryStream())
                using (var cStream = new CryptoStream(mStream, form, CryptoStreamMode.Write))
                {
                    cStream.Write(source, 0, source.Length);
                    cStream.FlushFinalBlock();
                    byte[] result = mStream.ToArray();
                    cStream.Close();
                    mStream.Close();
                    return result;
                }
            }
        }

        #endregion

        #region 私有方法：TripleDesDecrypt解密(3DES解密)
        /// <summary>
        /// 3Des解密，密钥长度必需是24字节
        /// </summary>
        /// <param name="key">密钥字节数组</param>
        /// <param name="iv">向量字节数组</param>
        /// <param name="source">加密后的字节数组</param>
        /// <param name="dataLen">解密后的数据长度</param>
        /// <returns>解密后的字节数组</returns>
        private static byte[] TripleDesDecrypt(byte[] key, byte[] iv, byte[] source, out int dataLen)
        {
            var length = source.Length;
            byte[] result = new byte[length];
            using (var dsp = new TripleDESCryptoServiceProvider())
            {
                dsp.Mode = CipherMode.CBC; // 默认值
                dsp.Padding = PaddingMode.PKCS7; // 默认值

                using (ICryptoTransform form = dsp.CreateDecryptor(key, iv))
                using (var mStream = new MemoryStream(source))
                using (var cStream = new CryptoStream(mStream, form, CryptoStreamMode.Read))
                {
                    dataLen = cStream.Read(result, 0, length);
                    cStream.Close();
                    mStream.Close();
                    return result;
                }
            }
        }

        /// <summary>
        /// 3Des解密，密钥长度必需是24字节
        /// </summary>
        /// <param name="key">密钥字节数组</param>
        /// <param name="iv">向量字节数组</param>
        /// <param name="source">加密后的字节数组</param>
        /// <returns>解密后的字节数组</returns>
        private static byte[] TripleDesDecrypt(byte[] key, byte[] iv, byte[] source)
        {
            byte[] result = TripleDesDecrypt(key, iv, source, out var dataLen);

            if (result.Length != dataLen)
            {
                // 如果数组长度不是解密后的实际长度，需要截断多余的数据，用来解决Gzip的"Magic byte doesn't match"的问题
                byte[] resultToReturn = new byte[dataLen];
                Array.Copy(result, resultToReturn, dataLen);
                return resultToReturn;
            }
            return result;
        }
        #endregion


        #endregion


        #region SHA1加密

        /// <summary>
        /// SHA1加密，等效于 PHP 的 SHA1() 代码
        /// </summary>
        /// <param name="source">被加密的字符串</param>
        /// <param name="encoding">编码格式，为空时使用UTF-8</param>
        /// <returns>加密后的字符串</returns>
        public static string SHA1_Encrypt(string source, Encoding encoding = null)
        {
            if (encoding == null)
                encoding = Utf8;
            byte[] temp1 = encoding.GetBytes(source);
            byte[] temp2;
            using (SHA1CryptoServiceProvider sha = new SHA1CryptoServiceProvider())
            {
                temp2 = sha.ComputeHash(temp1);
                sha.Clear();
            }

            //注意，不能用这个
            //string output = Convert.ToBase64String(temp2); 

            string output = BitConverter.ToString(temp2);
            output = output.Replace("-", "");
            output = output.ToLower();
            return output;
        }
        #endregion


        #region Base64编解码
        /// <summary>
        /// 编码 通过HTTP传递的Base64编码
        /// </summary>
        /// <param name="source">编码前的</param>
        /// <param name="encoding">编码格式，为空时使用UTF-8</param>
        /// <returns>编码后的</returns>
        public static string Base64Encode(string source, Encoding encoding = null)
        {
            if (encoding == null)
                encoding = Utf8;
            //空串处理
            if (string.IsNullOrEmpty(source))
            {
                return "";
            }

            //编码
            string encodeString = Convert.ToBase64String(encoding.GetBytes(source));

            //过滤
            encodeString = encodeString.Replace("+", "~");
            encodeString = encodeString.Replace("/", "@");
            encodeString = encodeString.Replace("=", "$");

            //返回
            return encodeString;
        }

        /// <summary>
        /// 解码 通过HTTP传递的Base64解码
        /// </summary>
        /// <param name="source">解码前的</param>
        /// <param name="encoding">编码格式，为空时使用UTF-8</param>
        /// <returns>解码后的</returns>
        public static string Base64Decode(string source, Encoding encoding = null)
        {
            if (encoding == null)
                encoding = Utf8;
            //空串处理
            if (string.IsNullOrEmpty(source))
            {
                return "";
            }

            //还原
            string deocdeString = source;
            deocdeString = deocdeString.Replace("~", "+");
            deocdeString = deocdeString.Replace("@", "/");
            deocdeString = deocdeString.Replace("$", "=");

            //Base64解码
            deocdeString = encoding.GetString(Convert.FromBase64String(deocdeString));

            //返回
            return deocdeString;
        }
        #endregion


        #region Aes加解密

        /// <summary>
        /// AES加密
        /// </summary>
        /// <param name="source">被加密的字符串</param>
        /// <param name="key">加密的密钥</param>
        /// <param name="encoding">编码格式，为空时使用UTF-8</param>
        /// <returns>加密后的字符串</returns>
        public static string AES_Encrypt(string source, string key = null, Encoding encoding = null)
        {
            if (encoding == null)
                encoding = Utf8;
            if (string.IsNullOrEmpty(key))
                key = AES_KEY;

            byte[] inputBuffers = encoding.GetBytes(source);
            byte[] keyBytes = GetKeyByte(key, 32, encoding);
            byte[] results;
            using (var aesProvider = new AesCryptoServiceProvider())
            {
                aesProvider.Key = keyBytes;
                aesProvider.Mode = CipherMode.ECB;
                aesProvider.Padding = PaddingMode.PKCS7;
                using (ICryptoTransform cryptoTransform = aesProvider.CreateEncryptor())
                {
                    results = cryptoTransform.TransformFinalBlock(inputBuffers, 0, inputBuffers.Length);
                    aesProvider.Clear();
                }
            }
            return Convert.ToBase64String(results); 
        }

        /// <summary>
        /// AES解密
        /// </summary>
        /// <param name="input">待解密的字符串</param>
        /// <param name="key">解密的密钥</param>
        /// <param name="encoding">编码格式，为空时使用UTF-8</param>
        /// <returns>解密后的字符串</returns>
        public static string AES_Decrypt(string input, string key = null, Encoding encoding = null)
        {
            if (encoding == null)
                encoding = Utf8;
            if (string.IsNullOrEmpty(key))
                key = AES_KEY;

            byte[] inputBuffers = Convert.FromBase64String(input);
            byte[] keyBytes = GetKeyByte(key, 32, encoding);
            byte[] results;
            using (var aesProvider = new AesCryptoServiceProvider())
            {
                aesProvider.Key = keyBytes;
                aesProvider.Mode = CipherMode.ECB;
                aesProvider.Padding = PaddingMode.PKCS7;
                using (ICryptoTransform cryptoTransform = aesProvider.CreateDecryptor())
                {
                    results = cryptoTransform.TransformFinalBlock(inputBuffers, 0, inputBuffers.Length);
                    aesProvider.Clear();
                }
            }
            return encoding.GetString(results);
        }

        #endregion


        #region HA256加密

        /// <summary>
        /// HMACSHA256 加密，可用于jwt签名计算
        /// </summary>
        /// <param name="source">被加密的字符串</param>
        /// <param name="key">要使用的加密key</param>
        /// <param name="encoding">编码格式，为空时使用UTF-8</param>
        /// <returns>加密后的字符串</returns>
        public static string SHA256_Encrypt(string source, string key = null, Encoding encoding = null)
        {
            if (encoding == null)
                encoding = Utf8;
            if (string.IsNullOrEmpty(key))
            {
                key = Key;
            }
            byte[] temp1 = encoding.GetBytes(source);
            byte[] temp2;
            using (var s256 = new HMACSHA256(encoding.GetBytes(key)))
            {
                temp2 = s256.ComputeHash(temp1);
            }
            string output = Convert.ToBase64String(temp2); 
            return output;
        }
        #endregion


        private static byte[] GetKeyByte(string key, int needLen, Encoding encoding)
        {
            int len = key.Length;
            // 太长时，截取
            if (len > needLen)
                key = key.Substring(0, needLen);
            // 不足时，用小数点补足
            else if (len < needLen)
                key = key.PadRight(needLen, '.');
            return encoding.GetBytes(key);
        }
    }
}

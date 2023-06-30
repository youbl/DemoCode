using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Beinet.Core.FileExt;
using Beinet.Core.Util;

namespace Beinet.Core.CryptExt
{
    /// <summary>
    /// ���ܰ����� 
    /// </summary>
    public class CryptoHelper
    {
        private static readonly Encoding Utf8 = FileHelper.UTF8_NoBom;

        private static string _key;
        /// <summary>
        /// DES�ӽ��ܵ�Ĭ����Կ������, 8λ�ַ�
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
        /// 3DES�ӽ��ܵ�Ĭ����Կ, 24λ�ַ�, ǰ8λ��Ϊ����
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
        /// AES�ӽ��ܵ�Ĭ����Կ, 32λ�ַ�
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

        #region  MD5����

        /// <summary>
        /// ��׼MD5����
        /// </summary>
        /// <param name="source">�������ַ���</param>
        /// <param name="addKey">�����ַ���</param>
        /// <param name="encoding">���뷽ʽ��Ϊ��ʱʹ��UTF-8</param>
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
        /// �����ļ���MD5ֵ������
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns>����Сдmd5</returns>
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
        
        
        #region  DES �ӽ���
        /// <summary>
        /// Desc����
        /// </summary>
        /// <param name="source">�������ַ�</param>
        /// <param name="key">��Կ</param>
        /// <param name="encoding">�����ʽ��Ϊ��ʱʹ��UTF-8</param>
        /// <returns>string</returns>
        public static string DES_Encrypt(string source, string key = null, Encoding encoding = null)
        {
            if (string.IsNullOrEmpty(source))
                return null;
            if (string.IsNullOrEmpty(key))
                key = Key;
            if (encoding == null)
                encoding = Utf8;

            //���ַ����ŵ�byte������  
            byte[] inputByteArray = encoding.GetBytes(source);

            // ��Կ������8λ������ᱨ�� System.ArgumentException: ָ�����Ĵ�С���ڴ��㷨��Ч��
            var keyByte = GetKeyByte(key, 8, encoding);
            var ivByte = keyByte;

            byte[] result;
            using (DESCryptoServiceProvider des = new DESCryptoServiceProvider())
            {
                //�������ܶ������Կ��ƫ����
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
        /// DES����
        /// </summary>
        /// <param name="source">����</param>
        /// <param name="key">��Կ</param>
        /// <param name="encoding">�����ʽ��Ϊ��ʱʹ��UTF-8</param>
        /// <returns>����</returns>
        public static string DES_Decrypt(string source, string key = null, Encoding encoding = null)
        {
            if (string.IsNullOrEmpty(source))
                return null;
            if (string.IsNullOrEmpty(key))
                key = Key;
            if (encoding == null)
                encoding = Utf8;

            //���ַ���תΪ�ֽ�����  
            byte[] inputByteArray = Convert.FromBase64String(source);
            // ��Կ������8λ������ᱨ�� System.ArgumentException: ָ�����Ĵ�С���ڴ��㷨��Ч��
            var keyByte = GetKeyByte(key, 8, encoding);
            var ivByte = keyByte;

            byte[] result;
            using (DESCryptoServiceProvider des = new DESCryptoServiceProvider())
            {
                //�������ܶ������Կ��ƫ��������ֵ��Ҫ�������޸�  
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


        #region 3DES�ӽ���
        /// <summary>
        /// ʹ��ָ����key��iv������input����
        /// </summary>
        /// <param name="input">�����ܵ��ַ���</param>
        /// <param name="encoding">�����ʽ��Ĭ��ΪUTF8</param>
        /// <param name="key">��Կ������Ϊ24λ����</param>
        /// <param name="iv">����������Ϊ8λ����</param>
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

            // ��ȡ���ܺ���ֽ�����
            byte[] arrData = encoding.GetBytes(input);
            byte[] result = TripleDesEncrypt(arrKey, arrIV, arrData);

            // ת��Ϊ16�����ַ���
            return Convert.ToBase64String(result);
            //return BitConverter.ToString(newSource).Replace("-", "").ToUpper();
        }


        /// <summary>
        /// ʹ��ָ����key��iv������input����
        /// </summary>
        /// <param name="input">�����ܵ��ַ���</param>
        /// <param name="encoding">�����ʽ��Ĭ��ΪUTF8</param>
        /// <param name="key">��Կ������Ϊ24λ����</param>
        /// <param name="iv">����������Ϊ8λ����</param>
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

            // ��ȡ���ܺ���ֽ�����
            byte[] arrData = Convert.FromBase64String(input);
            byte[] result = TripleDesDecrypt(arrKey, arrIV, arrData);
            return encoding.GetString(result);
        }


        #region ˽�з�����TripleDesEncrypt����(3DES����)
        /// <summary>
        /// 3Des���ܣ���Կ���ȱ�����24�ֽ�
        /// </summary>
        /// <param name="key">��Կ�ֽ�����</param>
        /// <param name="iv">�����ֽ�����</param>
        /// <param name="source">Դ�ֽ�����</param>
        /// <returns>���ܺ���ֽ�����</returns>
        private static byte[] TripleDesEncrypt(byte[] key, byte[] iv, byte[] source)
        {
            using (var dsp = new TripleDESCryptoServiceProvider())
            {
                dsp.Mode = CipherMode.CBC; // Ĭ��ֵ
                dsp.Padding = PaddingMode.PKCS7; // Ĭ��ֵ

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

        #region ˽�з�����TripleDesDecrypt����(3DES����)
        /// <summary>
        /// 3Des���ܣ���Կ���ȱ�����24�ֽ�
        /// </summary>
        /// <param name="key">��Կ�ֽ�����</param>
        /// <param name="iv">�����ֽ�����</param>
        /// <param name="source">���ܺ���ֽ�����</param>
        /// <param name="dataLen">���ܺ�����ݳ���</param>
        /// <returns>���ܺ���ֽ�����</returns>
        private static byte[] TripleDesDecrypt(byte[] key, byte[] iv, byte[] source, out int dataLen)
        {
            var length = source.Length;
            byte[] result = new byte[length];
            using (var dsp = new TripleDESCryptoServiceProvider())
            {
                dsp.Mode = CipherMode.CBC; // Ĭ��ֵ
                dsp.Padding = PaddingMode.PKCS7; // Ĭ��ֵ

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
        /// 3Des���ܣ���Կ���ȱ�����24�ֽ�
        /// </summary>
        /// <param name="key">��Կ�ֽ�����</param>
        /// <param name="iv">�����ֽ�����</param>
        /// <param name="source">���ܺ���ֽ�����</param>
        /// <returns>���ܺ���ֽ�����</returns>
        private static byte[] TripleDesDecrypt(byte[] key, byte[] iv, byte[] source)
        {
            byte[] result = TripleDesDecrypt(key, iv, source, out var dataLen);

            if (result.Length != dataLen)
            {
                // ������鳤�Ȳ��ǽ��ܺ��ʵ�ʳ��ȣ���Ҫ�ض϶�������ݣ��������Gzip��"Magic byte doesn't match"������
                byte[] resultToReturn = new byte[dataLen];
                Array.Copy(result, resultToReturn, dataLen);
                return resultToReturn;
            }
            return result;
        }
        #endregion


        #endregion


        #region SHA1����

        /// <summary>
        /// SHA1���ܣ���Ч�� PHP �� SHA1() ����
        /// </summary>
        /// <param name="source">�����ܵ��ַ���</param>
        /// <param name="encoding">�����ʽ��Ϊ��ʱʹ��UTF-8</param>
        /// <returns>���ܺ���ַ���</returns>
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

            //ע�⣬���������
            //string output = Convert.ToBase64String(temp2); 

            string output = BitConverter.ToString(temp2);
            output = output.Replace("-", "");
            output = output.ToLower();
            return output;
        }
        #endregion


        #region Base64�����
        /// <summary>
        /// ���� ͨ��HTTP���ݵ�Base64����
        /// </summary>
        /// <param name="source">����ǰ��</param>
        /// <param name="encoding">�����ʽ��Ϊ��ʱʹ��UTF-8</param>
        /// <returns>������</returns>
        public static string Base64Encode(string source, Encoding encoding = null)
        {
            if (encoding == null)
                encoding = Utf8;
            //�մ�����
            if (string.IsNullOrEmpty(source))
            {
                return "";
            }

            //����
            string encodeString = Convert.ToBase64String(encoding.GetBytes(source));

            //����
            encodeString = encodeString.Replace("+", "~");
            encodeString = encodeString.Replace("/", "@");
            encodeString = encodeString.Replace("=", "$");

            //����
            return encodeString;
        }

        /// <summary>
        /// ���� ͨ��HTTP���ݵ�Base64����
        /// </summary>
        /// <param name="source">����ǰ��</param>
        /// <param name="encoding">�����ʽ��Ϊ��ʱʹ��UTF-8</param>
        /// <returns>������</returns>
        public static string Base64Decode(string source, Encoding encoding = null)
        {
            if (encoding == null)
                encoding = Utf8;
            //�մ�����
            if (string.IsNullOrEmpty(source))
            {
                return "";
            }

            //��ԭ
            string deocdeString = source;
            deocdeString = deocdeString.Replace("~", "+");
            deocdeString = deocdeString.Replace("@", "/");
            deocdeString = deocdeString.Replace("$", "=");

            //Base64����
            deocdeString = encoding.GetString(Convert.FromBase64String(deocdeString));

            //����
            return deocdeString;
        }
        #endregion


        #region Aes�ӽ���

        /// <summary>
        /// AES����
        /// </summary>
        /// <param name="source">�����ܵ��ַ���</param>
        /// <param name="key">���ܵ���Կ</param>
        /// <param name="encoding">�����ʽ��Ϊ��ʱʹ��UTF-8</param>
        /// <returns>���ܺ���ַ���</returns>
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
        /// AES����
        /// </summary>
        /// <param name="input">�����ܵ��ַ���</param>
        /// <param name="key">���ܵ���Կ</param>
        /// <param name="encoding">�����ʽ��Ϊ��ʱʹ��UTF-8</param>
        /// <returns>���ܺ���ַ���</returns>
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


        #region HA256����

        /// <summary>
        /// HMACSHA256 ���ܣ�������jwtǩ������
        /// </summary>
        /// <param name="source">�����ܵ��ַ���</param>
        /// <param name="key">Ҫʹ�õļ���key</param>
        /// <param name="encoding">�����ʽ��Ϊ��ʱʹ��UTF-8</param>
        /// <returns>���ܺ���ַ���</returns>
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
            // ̫��ʱ����ȡ
            if (len > needLen)
                key = key.Substring(0, needLen);
            // ����ʱ����С���㲹��
            else if (len < needLen)
                key = key.PadRight(needLen, '.');
            return encoding.GetBytes(key);
        }
    }
}

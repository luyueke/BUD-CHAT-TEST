// @Author: YangJie
// @Description:
// @Date:  2023/07/28
// @Modify:

using System.Security.Cryptography;
using System.Text;

namespace Basic.Utils
{
    public static class HashUtils
    {

        public static string GetMd5Hash(this string str)
        {
            return Encoding.UTF8.GetBytes(str).GetMd5Hash();
        }

        public static string ToStr(this byte[] bytes)
        {
            return Encoding.UTF8.GetString(bytes);
        }

        public static string GetMd5Hash(this byte[] bytes)
        {
            return GetMd5Hash_Internal(bytes);
        }




        private static string GetMd5Hash_Internal(byte[] bytes)
        {
            using var md5 = MD5.Create();
            var hashBytes = md5.ComputeHash(bytes);
            var sb = new StringBuilder();

            foreach (var hashByte in hashBytes)
            {
                sb.Append(hashByte.ToString("x2"));
            }
            return sb.ToString().ToUpper();
        }
    }
}

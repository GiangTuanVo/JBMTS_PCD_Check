using System;
using System.Security.Cryptography;
using System.Text;

namespace JBMTS_PCD_Check.Model
{
    public class MD5Encryption
    {
        private static MD5Encryption instance;
        public static MD5Encryption Instance
        {
            get
            {
                if (instance == null)
                    instance = new MD5Encryption();

                return instance;
            }
            set
            {
                instance = value;
            }
        }

        public string encryptMD5(string pass)
        {
            MD5 md5 = new MD5CryptoServiceProvider();
            UTF8Encoding encoder = new UTF8Encoding();
            Byte[] originalBytes = encoder.GetBytes(pass);
            Byte[] encodedBytes = md5.ComputeHash(originalBytes);
            pass = BitConverter.ToString(encodedBytes).Replace("-", "");
            string result = pass.ToLower();
            return result;
        }
    }
}
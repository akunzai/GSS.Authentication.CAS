using System.Security.Cryptography;
using System.Text;
using Microsoft.Owin.Security.DataProtection;

namespace GSS.Authentication.CAS.Owin.Tests
{
    internal class AesDataProtector : IDataProtector
    {
        private readonly byte[] _key;
        private readonly byte[] _iv;

        public AesDataProtector(string key = null)
        {
            if (!string.IsNullOrWhiteSpace(key))
            {
                using var sha = SHA256.Create();
                _key = sha.ComputeHash(Encoding.UTF8.GetBytes(key));
            }
            using var aes = Aes.Create();
            if (_key == null)
            {
                _key = aes.Key;
            }
            _iv = aes.IV;
        }

        public byte[] Protect(byte[] userData)
        {
            using var aes = Aes.Create();
            aes.Key = _key;
            aes.IV = _iv;
            using var transform = aes.CreateEncryptor();
            return transform.TransformFinalBlock(userData, 0, userData.Length);
        }

        public byte[] Unprotect(byte[] protectedData)
        {
            using var aes = Aes.Create();
            aes.Key = _key;
            aes.IV = _iv;
            using var transform = aes.CreateDecryptor();
            return transform.TransformFinalBlock(protectedData, 0, protectedData.Length);
        }
    }
}

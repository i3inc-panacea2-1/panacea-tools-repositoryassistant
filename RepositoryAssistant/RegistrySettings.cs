using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace RepositoryAssistant
{
    public static class RegistrySettings
    {
        public static string ReadValue(string text, string name, bool secure = false)
        {
            using (var reg = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\PanaceaDev"))
            {
                string val = (string)reg.GetValue(name, null);
                if (val == null)
                {
                    Console.WriteLine(text);
                    val = Console.ReadLine();
                    if (secure)
                    {
                        val = EncryptData(val);
                    }
                    reg.SetValue(name, val);
                }
                if (secure)
                    return DecryptData(val);

                return val;
            }
        }


        public static string EncryptData(string data)
        {
            if (data == null)
                throw new ArgumentNullException("data");

            //encrypt data
            var encryptdata = Encoding.Unicode.GetBytes(data);
            byte[] encrypted = ProtectedData.Protect(encryptdata, null, DataProtectionScope.CurrentUser);

            //return as base64 string
            return Convert.ToBase64String(encrypted);
        }

        public static string DecryptData(string cipher)
        {
            if (cipher == null) throw new ArgumentNullException("cipher");

            //parse base64 string
            byte[] data = Convert.FromBase64String(cipher);

            //decrypt data
            byte[] decrypted = ProtectedData.Unprotect(data, null, DataProtectionScope.CurrentUser);
            return Encoding.Unicode.GetString(decrypted);
        }

        public static SecureString ToSecureString(this string plainString)
        {
            if (plainString == null)
                return null;

            SecureString secureString = new SecureString();
            foreach (char c in plainString.ToCharArray())
            {
                secureString.AppendChar(c);
            }
            return secureString;
        }
    }
}

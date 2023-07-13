using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;

namespace PartitionUtility
{
    public static class Checker
    {
        private static readonly object locker = new object();

        private static byte[] StringToByteArray(string text)
        {
            MemoryStream fs = new MemoryStream();
            BinaryFormatter formatter = new BinaryFormatter();
            try
            {
                lock (locker)
                {
                    formatter.Serialize(fs, text);
                }
                return fs.ToArray();
            }
            catch
            {
                return null;
            }
            finally
            {
                fs.Close();
            }
        }

        public static string GetFileHash(string fileName)
        {
            string text = File.ReadAllText(fileName, Encoding.UTF8);
            SHA256 sha256 = SHA256.Create();

            string hash = "";
            foreach (byte b in sha256.ComputeHash(StringToByteArray(text))) hash += b.ToString("x2").ToLower();
            return hash;
        }

        public static bool IsValid(string fileName)
        {
            string text = File.ReadAllText(fileName, Encoding.UTF8);
            if (text.Length < 70) return false;

            int index = text.IndexOf("\r\nCRC:");
            if (index == -1) return false;

            string config = text.Substring(0, index);
            string hash = text.Substring(index + 6);

            SHA256 sha256 = SHA256.Create();

            string fileHash = "";
            foreach (byte b in sha256.ComputeHash(StringToByteArray(config))) fileHash += b.ToString("x2").ToLower();

            File.WriteAllText(fileName, config);

            StringComparer comparer = StringComparer.OrdinalIgnoreCase;
            return comparer.Compare(hash, fileHash) == 0;
        }
    }
}

using System;
using System.Text;

namespace PartitionUtility
{
    public static class Security
    {
        public static string Protect(string str)
        {
            if (str == null) return "";
            byte[] data = Encoding.ASCII.GetBytes(str);
            string protectedData = Convert.ToBase64String(data);

            return protectedData;
        }

        public static string Unprotect(string str)
        {
            byte[] protectedData = Convert.FromBase64String(str);
            string data = Encoding.ASCII.GetString(protectedData);

            return data;
        }
    }
}

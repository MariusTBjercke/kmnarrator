using System;
using System.Security.Cryptography;
using System.Text;

namespace KMNarrator.Cache
{
    internal static class CacheKey
    {
        public static string Compute(string locale, string voiceId, string modelId, double speed, string normalizedText)
        {
            string raw = (locale ?? "enGB") + "|"
                + (voiceId ?? "") + "|"
                + (modelId ?? "") + "|"
                + speed.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) + "|"
                + (normalizedText ?? "");

            using (SHA256 sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(raw));
                return ToHex(hash);
            }
        }

        private static string ToHex(byte[] bytes)
        {
            var builder = new StringBuilder(bytes.Length * 2);
            for (int i = 0; i < bytes.Length; i++)
            {
                builder.Append(bytes[i].ToString("x2"));
            }

            return builder.ToString();
        }
    }
}

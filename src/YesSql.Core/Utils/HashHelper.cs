using System;
using System.Security.Cryptography;
using System.Text;

namespace YesSql.Utils
{
    public static class HashHelper
    {
        // Use of numeric characters is supported but dialects must be prefixed with a valid character for the dialect.
        private static readonly string _encode32Chars = "0123456789abcdefghjkmnpqrstvwxyz";

        /// <summary>
        /// Produces a 52 character hash with the supplied prefix prepended.
        /// The length of the prefix plus the 52 char hash must be less than the dialects maximum length.
        /// </summary>
        public static string HashName(string prefix, string name)
        {
            if (string.IsNullOrEmpty(prefix))
            {
                throw new ArgumentNullException("A prefix is required");
            }

            var bytes = Encoding.UTF8.GetBytes(name);
            var hashed = SHA256.HashData(bytes);

            var long1 = BitConverter.ToInt64(hashed, 0);
            var long2 = BitConverter.ToInt64(hashed, 8);
            var long3 = BitConverter.ToInt64(hashed, 16);
            var long4 = BitConverter.ToInt64(hashed, 24);

            return prefix + ToBase32(long1, long2, long3, long4);
        }

        private static string ToBase32(long long1, long long2, long long3, long long4)
        {
            var charBuffer = new char[52];

            var i = 0;

            i = EncodeSegment(charBuffer, i, long1);

            i = EncodeSegment(charBuffer, i, long2);

            i = EncodeSegment(charBuffer, i, long3);

            EncodeSegment(charBuffer, i, long4);

            return new string(charBuffer);
        }

        private static int EncodeSegment(char[] charBuffer, int startIndex, long lng)
        {
            charBuffer[startIndex] = _encode32Chars[(int)(lng >> 60) & 31];
            charBuffer[startIndex + 1] = _encode32Chars[(int)(lng >> 55) & 31];
            charBuffer[startIndex + 2] = _encode32Chars[(int)(lng >> 50) & 31];
            charBuffer[startIndex + 3] = _encode32Chars[(int)(lng >> 45) & 31];
            charBuffer[startIndex + 4] = _encode32Chars[(int)(lng >> 40) & 31];
            charBuffer[startIndex + 5] = _encode32Chars[(int)(lng >> 35) & 31];
            charBuffer[startIndex + 6] = _encode32Chars[(int)(lng >> 30) & 31];
            charBuffer[startIndex + 7] = _encode32Chars[(int)(lng >> 25) & 31];
            charBuffer[startIndex + 8] = _encode32Chars[(int)(lng >> 20) & 31];
            charBuffer[startIndex + 9] = _encode32Chars[(int)(lng >> 15) & 31];
            charBuffer[startIndex + 10] = _encode32Chars[(int)(lng >> 10) & 31];
            charBuffer[startIndex + 11] = _encode32Chars[(int)(lng >> 5) & 31];
            charBuffer[startIndex + 12] = _encode32Chars[(int)lng & 31];

            return startIndex + 13;
        }
    }
}

using System;
using System.Linq;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace LeanCloud.Realtime.Internal
{
    internal static class StringEngine
    {
        internal static string Random(this string str, int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789abcdefghijklmnopqrstuvwxyz";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        internal static string TempConvId<T>(this IEnumerable<T> objs)
        {
            var orderedBase64Strs = objs.Select(obj => Encoding.UTF8.ToBase64(obj.ToString())).OrderBy(a => a, StringComparer.Ordinal).ToArray();
            return "_tmp:" + string.Join("$", orderedBase64Strs);
        }

        internal static string ToBase64(this System.Text.Encoding encoding, string text)
        {
            if (text == null)
            {
                return null;
            }

            byte[] textAsBytes = encoding.GetBytes(text);
            return Convert.ToBase64String(textAsBytes);
        }
    }
}

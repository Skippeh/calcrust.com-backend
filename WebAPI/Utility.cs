using System;

namespace WebAPI
{
    internal static class Utility
    {
        public static string ToCamelCaseString(this object obj)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));

            string str = obj.ToString();

            if (str.Length <= 1)
                return str.ToLower();

            return str.Substring(0, 1).ToLower() + str.Substring(1);
        }

        // https://www.dotnetperls.com/levenshtein
        /// <summary>
        /// Compute the distance between two strings.
        /// </summary>
        public static int StringDistance(string s, string t)
        {
            int n = s.Length;
            int m = t.Length;
            int[,] d = new int[n + 1, m + 1];
            
            if (n == 0)
            {
                return m;
            }

            if (m == 0)
            {
                return n;
            }

            for (int i = 0; i <= n; d[i, 0] = i++)
            {
            }

            for (int j = 0; j <= m; d[0, j] = j++)
            {
            }

            for (int i = 1; i <= n; i++)
            {
                for (int j = 1; j <= m; j++)
                {
                    int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;
                    
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }

            return d[n, m];
        }
    }
}
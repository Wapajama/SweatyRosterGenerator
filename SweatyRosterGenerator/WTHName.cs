using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SweatyRosterGenerator
{
    public class WTHName
    {
        public static string RemoveSpecialCharacters(string str)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < str.Length; i++)
            {
                if ((str[i] >= '0' && str[i] <= '9')
                    || (str[i] >= 'A' && str[i] <= 'z'
                        || (str[i] == '.' || str[i] == '_')))
                {
                    sb.Append(str[i]);
                }
            }

            return sb.ToString();
        }

        public string Name { get { return wthName; } }

        static readonly string[] wthTitles = { "WTH", "WTHR" };
        static readonly char[] wthFirstBrackets = { '(', '[', '{', '|' };
        static readonly char[] wthLastBrackets = { ')', ']', '}', '|' };

        public bool MatchesAny(string name)
        {
            if (name == wthName) return true;
            if (name == baseName) return true;
            foreach (string alias in aliases)
            {
                if (name == alias) return true;
            }

            return false;
        }

        private string wthName; // The name in the Battle planner, including "(WTH)"
        private List<string> aliases; // The aliases associated with this name
        private string baseName; // the name without any special characters and "(WTH)"

        public void SetName(string name)
        {
            wthName = name;
            baseName = GetNameWithoutWTH(name);
            baseName = RemoveSpecialCharacters(baseName);
        }

        public void AddAlias(string alias)
        {
            aliases.Add(alias);
        }

        public static readonly int NotFound = -1;
        public static string GetNameWithoutWTH(string originalName)
        {
            int lastbracket = NotFound;

            foreach (char bracket in wthLastBrackets)
            {
                lastbracket = originalName.LastIndexOf(bracket);
                if (lastbracket != NotFound)
                {
                    break;
                }
            }

            string newName = originalName;
            if (lastbracket != NotFound)
            {
                newName = originalName.Substring(lastbracket+1);
                for (int i = newName.Length - 1; i >= 0; --i)
                {
                    if (char.IsWhiteSpace(newName[i]) || char.IsSymbol(newName[i]))
                    {
                        newName = newName.Remove(i, 1);
                    }
                }
            }

            
            return newName;
        }
    }
}

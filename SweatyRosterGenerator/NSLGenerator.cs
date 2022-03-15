using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace SweatyRosterGenerator
{

    struct RankedPlayer
    {
        public int rank;
        public string name;
        public int kills;
        public int minutesPlayed;
    }

    public class NSLGenerator
    {
        public Dictionary<string, double> FinalNslMap { get; }
        readonly int maxSpaces = 3;

        public NSLGenerator()
        {
            FinalNslMap = new Dictionary<string, double>();
            ReadScoreboards();
        }

        static readonly string[] forbiddenWeapons = { "Tank/Arty", "155MM HOWITZER" };
        bool PlayedTankOrArty(string line)
        {
            foreach (string weapon in forbiddenWeapons)
            {
                if (line.Contains(weapon))
                {
                    return true;
                }
            }
            return false;
        }

        int GetRank(string line)
        {
            // 1. store rank
            int whitespacePos = -1;
            for (int i = 1; i < 5; i++)
            {
                if (line[i] == ' ')
                {
                    whitespacePos = i;
                    break;
                }
            }

            int rank = -1;

            try
            {
                rank = int.Parse(line.Substring(1, whitespacePos - 1));
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            return rank;
        }

        string GetName(string line, out int startIndex)
        {
            string name = string.Empty;

            int firstIndexName = 0;

            // First scan past the rank
            while (line[firstIndexName] != ' ')
            {
                ++firstIndexName;
            }
            // Then scan past the spaces in between the rank and name
            while (line[firstIndexName] == ' ')
            {
                ++firstIndexName;
            }


            int lastIndexName = firstIndexName;
            int spaceCounter = 0;
            // if there are too many spaces, it means we have passed the name and are on the way to the next value

            while (spaceCounter < maxSpaces)
            {
                if (line[lastIndexName] == ' ')
                {
                    ++spaceCounter;
                }
                else
                {
                    spaceCounter = 0;
                }
                ++lastIndexName;
            }
            lastIndexName -= maxSpaces;
            name = line.Substring(firstIndexName, lastIndexName - firstIndexName);
            startIndex = lastIndexName;
            return name;
        }

        int GetKills(string line, int startIndex)
        {
            int kills = 0;

            int killsFirstIndex = startIndex;
            while (line[killsFirstIndex] == ' ')
            {
                ++killsFirstIndex;
            }
            int killsLastIndex = killsFirstIndex;
            while (line[killsLastIndex] != ' ')
            {
                ++killsLastIndex;
            }

            try
            {
                kills = int.Parse(line.Substring(killsFirstIndex, killsLastIndex - killsFirstIndex));
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            return kills;
        }

        int GetMinutesPlayed(string line)
        {
            int lineLength = line.Length;

            int hours = 0;
            int.TryParse(line.Substring(lineLength - 7, 1), out hours);

            int minutes = 0;
            int.TryParse(line.Substring(lineLength - 5, 2), out minutes);

            return hours * 60 + minutes;
        }

        bool IsWTHPlayer(string line)
        {
            if (line.Contains("WTH"))
            {
                return true;
            }
            return false;
        }

        double GenerateNSL(RankedPlayer player, int maxPlayers)
        {
            // Kills Per hour
            double hours = ((double)player.minutesPlayed) / 60.0;
            double kph = player.kills / hours;
            double nsl = ((maxPlayers + 1) - player.rank) - (100 - maxPlayers) + kph;
            return kph;
        }

        void ReadScoreboards()
        {
            Dictionary<string, List<double>> nslMap = new Dictionary<string, List<double>>();
            // Key: name, value: NSL, Normalise Skill Level
            foreach (String file in Directory.GetFiles("C:\\Users\\Kristoffer\\source\\repos\\SweatyRosterGenerator\\data"))
            {
                List<RankedPlayer> players = new List<RankedPlayer>();
                StreamReader reader = File.OpenText(file);

                string header = reader.ReadLine(); // first line is just header, ignore

                int rank = 0;
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    // First, make sure this aint some tank or arty wanker
                    if (PlayedTankOrArty(line)) continue;
                    if (!IsWTHPlayer(line)) continue;

                    int killStartIndex = 0;
                    string name = GetName(line, out killStartIndex);
                    int kills = GetKills(line, killStartIndex);
                    int minutesPlayed = GetMinutesPlayed(line);

                    // latecomers can get tae fuck
                    if (minutesPlayed >= 30)
                    {
                        players.Add(new RankedPlayer { rank = ++rank, name = name, kills = kills, minutesPlayed = minutesPlayed });
                    }
                }
                foreach (RankedPlayer player in players)
                {
                    double nsl = GenerateNSL(player, players.Count);
                    if (!nslMap.ContainsKey(player.name))
                    {
                        List<double> nsls = new List<double>();
                        nsls.Add(nsl);
                        nslMap.Add(player.name, nsls);
                    }
                    else
                    {
                        nslMap[player.name].Add(nsl);
                    }
                }
            }

            foreach (var nsl in nslMap)
            {
                double avgNsl = 0.0;
                nsl.Value.ForEach(x => avgNsl += x);
                avgNsl /= nsl.Value.Count;
                FinalNslMap.Add(nsl.Key, avgNsl);
            }

            var orderedList = FinalNslMap.ToList().OrderBy(x => x.Value).Reverse();
            foreach (var item in orderedList)
            {
                Console.WriteLine("{1} :\t {0}", item.Key, item.Value);
            }
        }

        private static void PrintTeam(Team team)
        {
            for (int i = 0; i < team.Squads.Count; i++)
            {
                Console.WriteLine($"Squad {i + 1}");
                Console.WriteLine($"SquadLead: {team.Squads[i].SquadLead.Name} ({team.Squads[i].SquadLead.Nsl})");
                for (int n = 0; n < team.Squads[i].SquadMembers.Count; n++)
                {
                    Console.WriteLine($"{team.Squads[i].SquadMembers[n].Nsl} | {team.Squads[i].SquadMembers[n].Name}");
                }
                Console.WriteLine("\n");
            }
        }

    }
}

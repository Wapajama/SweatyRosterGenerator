using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SweatyRosterGenerator
{
    public class NSLGenerator
    {
        public Dictionary<string, double> FinalNslMap { get; };


        public NSLGenerator()
        {
            FinalNslMap = new Dictionary<string, double>();
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
                finalNslMap.Add(nsl.Key, avgNsl);
            }

            int derp = 0;
            foreach (var item in finalNslMap)
            {
                WTHPlayer player = new WTHPlayer { Name = item.Key, Nsl = item.Value, Role = GameRole.infantryMan };

                if (IsWTHPlayer(player.Name))
                {
                    derp++;
                    Participants.Add(player);
                }
                if (derp >= 80)
                {
                    break;
                }
            }

            var orderedList = finalNslMap.ToList().OrderBy(x => x.Value).Reverse();
            foreach (var item in orderedList)
            {
                Console.WriteLine("{1} :\t {0}", item.Key, item.Value);
            }
        }

    }
}

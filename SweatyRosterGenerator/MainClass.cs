using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Apis;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Auth.OAuth2;
using System.IO;


namespace SweatyRosterGenerator
{
    public enum GameRole
    {
        squadLead,
        infantryMan,

        reconSpotter,
        reconSniper,

        tankDriver,
        tankGunner,
        tankCommander,

        artilleryGunner,

        PREFERREDROLE, // for access in spreadsheet only

        support,
        engineer,
        antiTank,
        machineGunner,
        assault,
        medic,
        automaticRifle,
        rifleman,

        COUNT,
    }
    public class RoleProficiencyEntry
    {
        public RoleProficiencyEntry()
        {
            RolePoints = new List<int>((int)GameRole.COUNT);
            for (int i = 0; i < (int)GameRole.COUNT; i++)
            {
                RolePoints.Add(0);
            }
        }

        public List<int> RolePoints { get; set; }
        public string PreferredRole { get; set; }
    }
    public class WTHPlayer : IComparable<WTHPlayer>
    {
        public string Name { get; set; }
        public double Nsl{ get; set; }
        public GameRole Role { get; set; }
        public int CompareTo(WTHPlayer other)
        {
            return other.Nsl.CompareTo(this.Nsl);
        }
    }
    public class Squad
    {
        public static readonly int MaxSquadMembers = 5;
        public Squad()
        {
            this.SquadMembers = new List<WTHPlayer>(MaxSquadMembers);
            this.SquadLead = null;
        }
        public WTHPlayer SquadLead { get; set; }
        public List<WTHPlayer> SquadMembers { get; set; }
        public double TotalNSL { get; set; }

    }
    public class Team
    {
        public WTHPlayer Commander { get; set; }
        public List<Squad> Squads { get; set; }

    }

    public class MainClass
    {
        static Dictionary<string, RoleProficiencyEntry> RoleProficiencies;
        static List<WTHPlayer> Participants;
        static List<WTHPlayer> SquadLeads;
        static readonly string[] Scopes = { SheetsService.Scope.Spreadsheets };
        static readonly string ApplicationName = "Sweaty Roster Generator";
        static readonly string SpreadsheetId = "1E6JfTNaelYbNvb-YRSeZ23DTA_ZqOQ7YDEpdQAI5Km8";
        static readonly string sheet = "Participants - Roles";
        
        // static Dictionary<string, double> finalNslMap = new Dictionary<string, double>();
        static SheetsService service;

        // REMOVE THIS
        static bool IsWTHPlayer(string line)
        {
            if (line.Contains("WTH"))
            {
                return true;
            }
            return false;
        }

        static void Main(string[] args)
        {
            NSLGenerator nslGenerator = new NSLGenerator();

            Participants = new List<WTHPlayer>();
            RoleProficiencies = new Dictionary<string, RoleProficiencyEntry>();
            ReadEntries();
            // ReadScoreboards();

            int derp = 0;
            foreach (var item in nslGenerator.FinalNslMap)
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

            CreateTeams();
            // TODO: make sure to even out squads to 5-6 members each
            // i.e no fewer than 4 squadmates (5 including squadlead)

            // TODO: Assign Squad lead weighted on proficiency, more stars likelier to be assigned

            // TODO: Check for Tank, arty, recon preference

            // TODO: Add a spreadsheet for time since last squadLeading, and weigh squadlead assignment based on it

            // TODO: Function for translating Teams to a sweaty roster
            Console.WriteLine("Hello derp");
            Console.ReadLine();
        }

        static int RoleProficiencyWidth = 18;
        static void ReadEntries()
        {
            GoogleCredential credential;
            using (var stream = new FileStream("sweatyroster.json", FileMode.Open, FileAccess.Read))
            {
                credential = GoogleCredential.FromStream(stream)
                    .CreateScoped(Scopes);
            }

            // Create Google Sheets API service.
            service = new SheetsService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName,
            });

            var range = $"{sheet}!C5:T477";
            SpreadsheetsResource.ValuesResource.GetRequest request =
                    service.Spreadsheets.Values.Get(SpreadsheetId, range);

            var response = request.Execute();
            IList<IList<object>> values = response.Values;
            if (values != null && values.Count > 0)
            {
                foreach (var row in values)
                {
                    // Console.WriteLine("{0} | {1} | {2} | {3} | {4} | {5}", row[0], row[1], row[2], row[3], row[4], row[5]);
                    RoleProficiencyEntry entry = new RoleProficiencyEntry();
                    string output = string.Empty;

                    for (int i = 1; i < RoleProficiencyWidth; i++)
                    {
                        output += $"{row[i]}";
                        for (int b = 0; b < 4 - row[i].ToString().Length; b++)
                        {
                            output += " ";
                        }
                        output += "|";
                    }
                    output += " " + row[0];

                    entry.PreferredRole = row[(int)GameRole.PREFERREDROLE + 1].ToString();
                    for (int i = 0; i < (int)GameRole.COUNT - 1; i++)
                    {
                        string pointsString = row[i + 1].ToString();
                        if (pointsString != "X")
                        {
                            entry.RolePoints[i] = pointsString.Length;
                        }
                    }

                    RoleProficiencies[row[0].ToString()] = entry;
                }
            }
            else
            {
                Console.WriteLine("No data found.");
            }
        }

        private static void CreateTeams()
        {
            HashSet<string> squadLeads = new HashSet<string>();
            foreach (var item in RoleProficiencies.AsEnumerable())
            {
                if (item.Value.PreferredRole == "SQUAD LEADER")
                {
                    squadLeads.Add(item.Key);
                }
            }

            // 1. How many players on each team
            int n_players_per_team1 = Participants.Count / 2;
            // if odd number, add one to the other team
            int n_players_per_team2 = (Participants.Count % 2) == 1 ? n_players_per_team1 : n_players_per_team1 + 1;

            // 2. Establish how many squads we needs to make room for all infantry
            int n_squads1 = (n_players_per_team1 / (Squad.MaxSquadMembers + 1)) + 1;
            int n_squads2 = (n_players_per_team2 / (Squad.MaxSquadMembers + 1)) + 1;

            Participants.Sort();
            Participants.Reverse();
            Team team1 = new Team();
            Team team2 = new Team();
            team1.Squads = new List<Squad>(n_squads1);
            team2.Squads = new List<Squad>(n_squads2);

            Stack<string> availableSQLs = new Stack<string>();

            for (int participant = Participants.Count - 1; participant >= 0; participant--)
            {
                if (squadLeads.Contains(Participants[participant].Name))
                {
                    availableSQLs.Push(Participants[participant].Name);
                    Participants.RemoveAt(participant);
                }
            }

            int originalSizeSqls = availableSQLs.Count;
            for (int i = 0; i < n_squads1; i++)
            {
                team1.Squads.Add(new Squad());
                if (availableSQLs.Count > originalSizeSqls / 2)
                {
                    WTHPlayer sql = null;
                    string sqlName = availableSQLs.Pop();
                    foreach (var item in Participants)
                    {
                        if (item.Name == sqlName)
                        {
                            sql = item;
                            break;
                        }
                    }
                    team1.Squads[team1.Squads.Count - 1].SquadLead = sql;
                }
            }

            for (int i = 0; i < n_squads2; i++)
            {
                team2.Squads.Add(new Squad());
                if (availableSQLs.Count > 0)
                {
                    WTHPlayer sql = null;
                    string sqlName = availableSQLs.Pop();
                    foreach (var item in Participants)
                    {
                        if (item.Name == sqlName)
                        {
                            sql = item;
                            break;
                        }
                    }
                    team2.Squads[team2.Squads.Count - 1].SquadLead = sql;
                }
            }


            int currentSquad1 = 0;
            int currentSquad2 = 0;
            for (int participant = Participants.Count - 1; participant >= 0; participant--)
            {
                bool even = (participant % 2) == 0;

                if (even)
                {
                    if (team1.Squads[currentSquad1].SquadLead != null)
                    {
                        team1.Squads[currentSquad1].SquadMembers.Add(Participants[participant]);
                    }
                    else
                    {
                        team1.Squads[currentSquad1].SquadLead = Participants[participant];
                    }
                    Participants.RemoveAt(participant);
                    if (team1.Squads[currentSquad1].SquadMembers.Count >= Squad.MaxSquadMembers)
                    {
                        currentSquad1++;
                    }
                }
                else
                {
                    if (team2.Squads[currentSquad2].SquadLead != null)
                    {
                        team2.Squads[currentSquad2].SquadMembers.Add(Participants[participant]);
                    }
                    else
                    {
                        team2.Squads[currentSquad2].SquadLead = Participants[participant];
                    }
                    Participants.RemoveAt(participant);
                    if (team2.Squads[currentSquad2].SquadMembers.Count >= Squad.MaxSquadMembers)
                    {
                        currentSquad2++;
                    }
                }
            }

            PrintTeam(team1);
            PrintTeam(team2);

        }
        private static void PrintTeam(Team team)
        {
            for (int i = 0; i < team.Squads.Count; i++)
            {
                Console.WriteLine($"Squad {i+1}");
                Console.WriteLine($"SquadLead: {team.Squads[i].SquadLead.Name} ({team.Squads[i].SquadLead.Nsl})");
                for (int n = 0; n < team.Squads[i].SquadMembers.Count; n++)
                {
                    Console.WriteLine($"{team.Squads[i].SquadMembers[n].Nsl} | {team.Squads[i].SquadMembers[n].Name}");
                }
                Console.WriteLine("\n");
            }
        }

        //struct RankedPlayer
        //{
        //    public int rank;
        //    public string name;
        //    public int kills;
        //    public int minutesPlayed;
        //}

        //static void ReadScoreboards()
        //{
        //    Dictionary<string, List<double>> nslMap = new Dictionary<string, List<double>>();
        //    // Key: name, value: NSL, Normalise Skill Level
        //    foreach (String file in Directory.GetFiles("C:\\Users\\Kristoffer\\source\\repos\\SweatyRosterGenerator\\data"))
        //    {
        //        List<RankedPlayer> players = new List<RankedPlayer>();
        //        StreamReader reader = File.OpenText(file);

        //        string header = reader.ReadLine(); // first line is just header, ignore

        //        int rank = 0;
        //        while (!reader.EndOfStream)
        //        {
        //            string line = reader.ReadLine();
        //            // First, make sure this aint some tank or arty wanker
        //            if (PlayedTankOrArty(line)) continue;
        //            if (!IsWTHPlayer(line)) continue;

        //            int killStartIndex = 0;
        //            string name = GetName(line, out killStartIndex);
        //            int kills = GetKills(line, killStartIndex);
        //            int minutesPlayed = GetMinutesPlayed(line);

        //            // latecomers can get tae fuck
        //            if (minutesPlayed >= 30)
        //            {
        //                players.Add(new RankedPlayer { rank = ++rank, name = name, kills = kills, minutesPlayed = minutesPlayed }); 
        //            }
        //        }
        //        foreach (RankedPlayer player in players)
        //        {
        //            double nsl = GenerateNSL(player, players.Count);
        //            if (!nslMap.ContainsKey(player.name))
        //            {
        //                List<double> nsls = new List<double>();
        //                nsls.Add(nsl);
        //                nslMap.Add(player.name, nsls); 
        //            }
        //            else
        //            {
        //                nslMap[player.name].Add(nsl);
        //            }
        //        }
        //    }

        //    foreach (var nsl in nslMap)
        //    {
        //        double avgNsl = 0.0;
        //        nsl.Value.ForEach(x => avgNsl += x);
        //        avgNsl /= nsl.Value.Count;
        //        finalNslMap.Add(nsl.Key, avgNsl);
        //    }

        //    int derp = 0;
        //    foreach (var item in finalNslMap)
        //    {
        //        WTHPlayer player = new WTHPlayer { Name = item.Key, Nsl = item.Value, Role = GameRole.infantryMan };

        //        if (IsWTHPlayer(player.Name))
        //        {
        //            derp++;
        //            Participants.Add(player); 
        //        }
        //        if (derp >= 80)
        //        {
        //            break;
        //        }
        //    }

        //    var orderedList = finalNslMap.ToList().OrderBy(x => x.Value).Reverse();
        //    foreach (var item in orderedList)
        //    {
        //        Console.WriteLine("{1} :\t {0}", item.Key, item.Value);
        //    }
        //}

        
    }
}

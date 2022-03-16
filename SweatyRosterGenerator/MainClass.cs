using System;
using System.Collections.Generic;
using System.Linq;
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
        
        static List<WTHPlayer> Participants;
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
            BattlePlannerInterface bpinterface = new BattlePlannerInterface();

            Participants = new List<WTHPlayer>();

            LoadParticipants(nslGenerator);
            CreateTeams(bpinterface);
            // TODO: make sure to even out squads to 5-6 members each
            // i.e no fewer than 4 squadmates (5 including squadlead)

            // TODO: Assign Squad lead weighted on proficiency, more stars likelier to be assigned

            // TODO: Check for Tank, arty, recon preference

            // TODO: Add a spreadsheet for time since last squadLeading, and weigh squadlead assignment based on it

            // TODO: Function for translating Teams to a sweaty roster
            Console.WriteLine("Hello derp");
            Console.ReadLine();
        }

        static string ParticipantsPath = "C:\\Users\\Kristoffer\\source\\repos\\SweatyRosterGenerator\\data\\participants.txt";
        private static void LoadParticipants(NSLGenerator nslGenerator)
        {
            StreamReader reader = File.OpenText(ParticipantsPath);
            List<string> names = new List<string>();
            while (!reader.EndOfStream)
            {
                names.Add(WTHName.GetNameWithoutWTH( reader.ReadLine().ToLower()));
            }

            foreach (string name in names)
            {
                //if (nslGenerator.FinalNslMap.ContainsKey(name))
                double nsl = 0.0;
                if (nslGenerator.GetPlayerByName(name, out nsl)) 
                {
                    WTHPlayer player = new WTHPlayer { Name = name, Nsl = nsl, Role = GameRole.infantryMan };
                    Participants.Add(player);
                }
                else
                {
                    Console.WriteLine($"{name} could not be found in the participant list.");
                }
            }

            //foreach (var item in nslGenerator.FinalNslMap)
            //{
            //    WTHPlayer player = new WTHPlayer { Name = item.Key, Nsl = item.Value, Role = GameRole.infantryMan };
            //    if (IsWTHPlayer(player.Name))
            //    {
            //        derp++;
            //        Participants.Add(player);
            //    }
            //    if (derp >= 80)
            //    {
            //        break;
            //    }
            //}
        }

        private static void CreateTeams(BattlePlannerInterface bpinterface)
        {
            HashSet<string> squadLeads = new HashSet<string>();

            foreach (var item in bpinterface.RoleProficiencies.AsEnumerable())
            {
                if (item.Value.PreferredRole == "SQUAD LEADER")
                {
                    squadLeads.Add(item.Key.ToLower());
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

            List<WTHPlayer> availableSQLsTemp = new List<WTHPlayer>();

            for (int participant = Participants.Count - 1; participant >= 0; participant--)
            {
                if (squadLeads.Contains(Participants[participant].Name))
                {
                    availableSQLsTemp.Add(Participants[participant]);
                    Participants.RemoveAt(participant);
                }
            }

            int playersPerSquads1 = n_players_per_team1 / n_squads1;
            int playersPerSquads2 = n_players_per_team2 / n_squads2;

            int mod1 = n_players_per_team1 % playersPerSquads1;
            int mod2 = n_players_per_team2 % playersPerSquads2;

            availableSQLsTemp.Sort();
            availableSQLsTemp.Reverse();
            Stack<WTHPlayer> availableSQLs = new Stack<WTHPlayer>();

            availableSQLsTemp.ForEach(x => availableSQLs.Push(x));

            int originalSizeSqls = availableSQLs.Count;
            for (int i = 0; i < n_squads1; i++)
            {
                team1.Squads.Add(new Squad());
                //if (availableSQLs.Count > originalSizeSqls / 2)
                //{
                //    WTHPlayer sql = availableSQLs.Pop();
                //    team1.Squads[team1.Squads.Count - 1].SquadLead = sql;
                //}
            }

            for (int i = 0; i < n_squads2; i++)
            {
                team2.Squads.Add(new Squad());
                //if (availableSQLs.Count > 0)
                //{
                //    WTHPlayer sql = availableSQLs.Pop();
                //    team2.Squads[team2.Squads.Count - 1].SquadLead = sql;
                //}
            }

            int currentSquad1 = 0;
            int currentSquad2 = 0;
            for (int participant = Participants.Count - 1; participant >= 0; participant--)
            {
                bool even = (participant % 2) == 0;

                if (even)
                {
                    if (availableSQLs.Count > 0 && team1.Squads[currentSquad1].SquadLead == null)
                    {
                        team1.Squads[currentSquad1].SquadLead = availableSQLs.Pop();
                    }
                    if (team1.Squads[currentSquad1].SquadLead != null)
                    {
                        team1.Squads[currentSquad1].SquadMembers.Add(Participants[participant]);
                    }
                    else
                    {
                        team1.Squads[currentSquad1].SquadLead = Participants[participant];
                    }
                    Participants.RemoveAt(participant);
                    if (mod1-- > 0)
                    {
                        if (team1.Squads[currentSquad1].SquadMembers.Count >= Squad.MaxSquadMembers)
                        {
                            currentSquad1++;
                        }
                    }
                    else
                    {
                        if (team1.Squads[currentSquad1].SquadMembers.Count >= playersPerSquads1)
                        {
                            currentSquad1++;
                        }
                    }
                    
                }
                else
                {
                    if (availableSQLs.Count > 0 && team2.Squads[currentSquad2].SquadLead == null)
                    {
                        team2.Squads[currentSquad2].SquadLead = availableSQLs.Pop();
                    }
                    if (team2.Squads[currentSquad2].SquadLead != null)
                    {
                        team2.Squads[currentSquad2].SquadMembers.Add(Participants[participant]);
                    }
                    else
                    {
                        team2.Squads[currentSquad2].SquadLead = Participants[participant];
                    }
                    Participants.RemoveAt(participant);
                    if (mod2-- > 0)
                    {
                        if (team2.Squads[currentSquad2].SquadMembers.Count >= Squad.MaxSquadMembers)
                        {
                            currentSquad2++;
                        }
                    }
                    else
                    {
                        if (team2.Squads[currentSquad2].SquadMembers.Count >= playersPerSquads2)
                        {
                            currentSquad2++;
                        }
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
                if (team.Squads[i].SquadLead != null)
                {
                    Console.WriteLine($"SquadLead: {team.Squads[i].SquadLead.Name} ({team.Squads[i].SquadLead.Nsl})"); 
                }
                for (int n = 0; n < team.Squads[i].SquadMembers.Count; n++)
                {
                    Console.WriteLine($"{team.Squads[i].SquadMembers[n].Nsl} | {team.Squads[i].SquadMembers[n].Name}");
                }
                Console.WriteLine("\n");
            }
        }
    }
}

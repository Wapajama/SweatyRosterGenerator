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

    public class BattlePlannerInterface
    {

        private readonly string[] Scopes = { SheetsService.Scope.Spreadsheets };
        private readonly string ApplicationName = "Sweaty Roster Generator";
        private readonly string SpreadsheetId = "1E6JfTNaelYbNvb-YRSeZ23DTA_ZqOQ7YDEpdQAI5Km8";
        private readonly string sheet = "Participants - Roles";

        // static Dictionary<string, double> finalNslMap = new Dictionary<string, double>();
        private SheetsService service;
        int RoleProficiencyWidth = 18;
        public Dictionary<string, RoleProficiencyEntry> RoleProficiencies { get; }
        public BattlePlannerInterface()
        {
            RoleProficiencies = new Dictionary<string, RoleProficiencyEntry>();
            ReadEntries();
        }

        void ReadEntries()
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
                    RoleProficiencyEntry entry = new RoleProficiencyEntry();

                    entry.PreferredRole = row[(int)GameRole.PREFERREDROLE + 1].ToString();
                    for (int i = 0; i < (int)GameRole.COUNT - 1; i++)
                    {
                        string pointsString = row[i + 1].ToString();
                        if (pointsString != "X")
                        {
                            entry.RolePoints[i] = pointsString.Length;
                        }
                    }

                    RoleProficiencies[WTHName.GetNameWithoutWTH( row[0].ToString())] = entry;
                }
            }
            else
            {
                Console.WriteLine("No data found.");
            }
        }


    }
}

using Arco.Services;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Collections.Generic;

namespace Arco.Models
{
    public class SystemConfig
    {
        private static SystemConfig _instance;
        private SystemConfig()
        {
            List<BsonDocument> configs = DataService.GetInstance().Database.GetCollection<BsonDocument>("systemconfig").Find("{}").ToList();
            if (configs != null && configs.Count > 0)
            {
                BsonDocument info = configs[0];
                if (info != null)
                {
                    Pbxmode = (string)info["pbxmode"];
                    ExtFrom = (int)info["extFrom"];
                    ExtTo = (int)info["extTo"];
                    MeetingFrom = (int)info["meetingFrom"];
                    MeetingTo = (int)info["meetingTo"];
                    BroadcastingFrom = (int)info["broadcastingFrom"];
                    BroadcastingTo = (int)info["broadcastingTo"];
                    Calltimeout = (int)info["calltimeout"];
                    Rollcalltimeout = (int)info["rollcalltimeout"];
                    Contactlimit = (int)info["contactlimit"];
                    License = (string)info["license"];
                }
            }
        }

        public static SystemConfig Instance
        {
            get
            {
                if (_instance == null) _instance = new SystemConfig();
                return _instance;
            }
        }
        public string Pbxmode { get; set; }
        public int ExtFrom { get; set; }
        public int ExtTo { get; set; }
        public int MeetingFrom { get; set; }
        public int MeetingTo { get; set; }
        public int BroadcastingFrom { get; set; }
        public int BroadcastingTo { get; set; }
        public int Calltimeout { get; set; }
        public int Rollcalltimeout { get; set; }
        public int Contactlimit { get; set; }

        public string License { get; set; }

    }
}

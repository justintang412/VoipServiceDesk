using Arco.Core;
using Arco.Models;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Arco.Services
{
    public class DataService
    {

        private static DataService _instance = null;
        private HttpClient HttpClient { get; set; }

        public string DataFolder { get; set; }
        public string Token { get; set; }
        public string Ip { get; set; }
        public string Port { get; set; }

        public string Version { get; set; }
        private DataService()
        {
            MongoClient mongoClient = new MongoClient("mongodb://127.0.0.1:27017");
            Database = mongoClient.GetDatabase("arco");

            HttpClient = new HttpClient();
            DataFolder = Environment.CurrentDirectory + "/data/";
            if (!Directory.Exists(DataFolder))
            {
                Directory.CreateDirectory(DataFolder);
            }
        }

        public static DataService GetInstance()
        {
            if (_instance == null)
            {
                try
                {
                    _instance = new DataService();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.StackTrace);
                }
            }
            return _instance;
        }

        public async Task<bool> HandupExt(string ext)
        {
            if (Convert.ToInt32(ext) >= Models.SystemConfig.Instance.ExtFrom &&
                   Convert.ToInt32(ext) <= Models.SystemConfig.Instance.ExtTo)
            {
                string json = "{ \"extid\": \"" + ext + "\" }";

                JObject response = await DataService.GetInstance().PostAsync(json, "/extension/hangup", true);

                if (response != null && ((string)response["status"]).Equals("Success"))
                {
                    return true;
                }
            }
            return false;

        }

        public async Task<bool> MakeCall(string from, string to)
        {
            if (to == null || to.Length == 0) return false;
            string uri = "";
            string json = "";
            if (Convert.ToInt32(to) >= Models.SystemConfig.Instance.ExtFrom &&
                    Convert.ToInt32(to) <= Models.SystemConfig.Instance.ExtTo)
            {
                uri = "/extension/dial_extension";
                json = "{" +
                    "\"caller\": \"" + from + "\"," +
                    "\"callee\": \"" + to + "\"," +
                    "\"autoanswer\": \"no\"" +
                    "}";
            }
            else
            {
                uri = "/extension/dial_outbound";
                json = "{" +
                    "\"extid\": \"" + from + "\"," +
                    "\"outto\": \"" + to + "\"," +
                    "\"autoanswer\": \"no\"" +
                    "}";
            }

            JObject response = await PostAsync(json, uri, true);

            if (response != null && ((string)response["status"]).Equals("Success"))
            {
                return true;
            }
            return false;
        }

        public async Task DownloadRecording(string url, string filename)
        {

            using (var request = new HttpRequestMessage(HttpMethod.Get, "http://" + Ip + ":" + Port + "/api/" + Version + url))
            {
                using (
                    Stream contentStream = await (await HttpClient.SendAsync(request)).Content.ReadAsStreamAsync(),
                    stream = new FileStream(DataFolder + filename, FileMode.Create, FileAccess.Write, FileShare.None, 102400, true))
                {
                    await contentStream.CopyToAsync(stream);
                }
            }
        }
        public async Task DownloadCDRAsync()
        {
            string starttime = null;
            string endtime = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
            IMongoCollection<BsonDocument> configCollection = Database.GetCollection<BsonDocument>("config");
            if (configCollection.CountDocuments("{}") == 0) {
                configCollection.InsertOne(new BsonDocument { { "name", "History_Download" },{ "endtime", endtime } });
                starttime = DateTime.UtcNow.AddDays(-1).ToString("yyyy-MM-dd HH:mm:ss");
            }
            else
            {
                BsonDocument config = configCollection .Find(Builders<BsonDocument>.Filter.Eq("name", "History_Download")).First();
                starttime = (string)config["endtime"];
            }
            
            

            if (endtime.CompareTo(starttime) > 0)
            {
                string json = "{ \"extid\": \"all\", \"starttime\": \"" + starttime + "\", \"endtime\": \"" + endtime + "\" }";
                JObject response = await DataService.GetInstance().PostAsync(json, "/cdr/get_random", true);

                await Task.Run(async () =>
                {
                    if (response != null)
                    {
                        if (((string)response["status"]).Equals("Success"))
                        {
                            string random = (string)response["random"];
                            string _stime = (string)response["starttime"];
                            string _etime = (string)response["endtime"];
                            string uri = "http://" + Ip + ":" + Port + "/api/v1.1.0/cdr/download?extid=all&starttime=" + _stime + "&endtime=" + _etime + "&token=" + this.Token + "&random=" + random;
                            string guid = Guid.NewGuid().ToString();
                            string filename = DataFolder + guid + ".csv";
                            using (var request = new HttpRequestMessage(HttpMethod.Get, uri))
                            {
                                using (
                                    Stream contentStream = await (await HttpClient.SendAsync(request)).Content.ReadAsStreamAsync(),
                                    stream = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.None, 102400, true))
                                {
                                    await contentStream.CopyToAsync(stream);
                                }
                            }
                            StreamReader file = new StreamReader(filename);
                            string line = file.ReadLine();
                            if (line != null)
                            {
                                List<BsonDocument> data = new List<BsonDocument>();
                                while ((line = file.ReadLine()) != null)
                                {
                                    string[] cdr = line.Split(',');
                                    data.Add(BsonDocument.Parse(
                                    "{" +
                                    "\"callid\": \"" + cdr[0] + "\"," +
                                    "\"timestart\": \"" + cdr[1].ToGMTZones(8) + "\"," +
                                    "\"callfrom\": \"" + cdr[2] + "\"," +
                                    "\"callto\": \"" + cdr[3] + "\"," +
                                    "\"callduraction\": \"" + cdr[4] + "\"," +
                                    "\"talkduraction\": \"" + cdr[5] + "\"," +
                                    "\"status\": \"" + cdr[8] + "\"," +
                                    "\"type\": \"" + cdr[9] + "\"," +
                                    "\"recording\": \"" + cdr[11] + "\"," +
                                    "\"sn\": \"" + cdr[13] + "\"" +
                                    "}"));
                                }
                                Database.GetCollection<BsonDocument>("history").InsertMany(data);
                            }
                            file.Close();
                            Database.GetCollection<BsonDocument>("config").UpdateOne(
                                Builders<BsonDocument>.Filter.Eq("name", "History_Download"),
                                Builders<BsonDocument>.Update.Set("endtime", endtime));
                        }
                    }
                });


            }
        }
        public void Log(string user, string module, string menu, string action, string description)
        {
            Database.GetCollection<BsonDocument>("log").InsertOneAsync(
                new BsonDocument {
                    { "user", user },
                    { "module", module },
                    { "menu", menu},
                    {"action", action },
                    { "description", description },
                    { "logtime", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                    } });
        }
        public void PutMessage(string type, string message)
        {
            Database.GetCollection<BsonDocument>("queue").InsertOneAsync(new BsonDocument { { "type", type }, { "value", message }, { "create_time", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() } });
        }

        public void ClearQueue()
        {
            Database.GetCollection<BsonDocument>("queue").DeleteMany(Builders<BsonDocument>.Filter.Empty);
        }
        public BsonDocument GetMessage()
        {
            SortDefinition<BsonDocument> sort = Builders<BsonDocument>.Sort.Ascending("create_time");
            List<BsonDocument> docs = Database.GetCollection<BsonDocument>("queue").Find(new BsonDocument()).Sort(sort).Limit(1).ToList();
            if (docs == null || docs.Count == 0)
            {
                return null;
            }
            else
            {
                Database.GetCollection<BsonDocument>("queue").DeleteOne(docs[0]);
                return docs[0];
            }
        }
        public void Close()
        {

        }

        public IMongoDatabase Database { get; set; }
        public void InsertManyAsync(string collection, List<BsonDocument> data)
        {
            Database.GetCollection<BsonDocument>(collection).InsertManyAsync(data);
        }
        public void InsertAsync(string collection, BsonDocument data)
        {
            Database.GetCollection<BsonDocument>(collection).InsertOneAsync(data);
        }

        public List<Group> QueryGroups(List<Contact> baseContacts)
        {
            return Database.GetCollection<BsonDocument>("groups").Find(new BsonDocument())
              .Sort("{rank: 1}")
              .ToList()
              .Select(x =>
              {
                  Group group = new Group
                  {
                      Groupid = (string)x["groupid"]??"",
                      Name = (string)x["name"] ?? "",
                      Number = (string)x["number"] ?? "",
                      Duplex = ((string)x["duplex"]).ToReadableGroupType(),
                      EnableKeyHanup = (string)x["enablekeyhanup"] ?? ""
                  };
                  string extensions = (string)x["allowexten"];
                  if (extensions != null)
                  {
                      group.AllowExten = new ObservableCollection<Contact>(
                      extensions.Split(',').Select(ext =>
                      {
                          string _ext = (string)ext;
                          Contact contact = baseContacts.Find(y => y.Devices.Find(d => d.DeviceType.Name.Equals("分机")).Number.Equals(_ext));
                          return contact;
                      }).ToList()
                      );
                  }

                  return group;
              }).ToList();
        }
        public ObservableCollection<Plan> QueryPlans(MainWindowViewModel _viewModel)
        {
            return new ObservableCollection<Plan>(Database.GetCollection<BsonDocument>("plans").Find(new BsonDocument())
              .Sort("{_id: -1}")
              .ToList()
              .Select(x =>
              {
                  Plan plan = new Plan
                  {
                      Name = (string)x["name"]??"",
                      Number = (string)x["number"]??"",
                      Content = (string)x["content"]??""
                  };
                  ObservableCollection<ContactWithSingleDevice> members = new ObservableCollection<ContactWithSingleDevice>(
                      ((BsonArray)x["members"]).Select(ext =>
                      {
                          string _ext = (string)ext;
                          ContactWithSingleDevice cwd = _ext.SetupContact(_viewModel);
                          return cwd;
                      }).ToList()
                      );

                  plan.Members = members;
                  return plan;
              }).ToList());
        }
        public ObservableCollection<Meeting> QueryMeetings(MainWindowViewModel _viewModel)
        {
            return new ObservableCollection<Meeting>(Database.GetCollection<BsonDocument>("meetings").Find(new BsonDocument())
              .Sort("{_id: -1}")
              .ToList()
              .Select(x =>
              {
                  Meeting meeting = new Meeting
                  {
                      Name = (string)x["name"]??"",
                      Number = (string)x["number"]??"",
                      Content = (string)x["content"]??"",
                      Starttime = (string)x["starttime"]??""
                  };
                  ObservableCollection<ContactWithSingleDevice> members = new ObservableCollection<ContactWithSingleDevice>(
                      ((BsonArray)x["members"]).Select(ext =>
                      {
                          string _ext = (string)ext;
                          ContactWithSingleDevice cwd = _ext.SetupContact(_viewModel);
                          return cwd;
                      }).ToList()
                      );

                  meeting.Members = members;
                  return meeting;
              }).ToList());
        }
        public async Task<JObject> PostAsync(string json, string uri, bool withToken)
        {
            if (withToken)
            {
                uri += "?token=" + this.Token;
            }
            StringContent data = new StringContent(json, Encoding.UTF8, "application/json");
            try
            {
                HttpResponseMessage response = await HttpClient.PostAsync("http://" + Ip + ":" + Port + "/api/" + Version + uri, data);
                string content = response.Content.ReadAsStringAsync().Result;
                JObject result = JsonConvert.DeserializeObject<JObject>(content);
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
            }
            return null;
        }
    }
}

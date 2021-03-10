using Arco.Core;
using Arco.Models;
using Arco.Services;
using Arco.Views;
using MahApps.Metro.Controls.Dialogs;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Arco
{
    /// <summary>
    /// this is the primary model for the whole app
    /// the first content child in mainwidow.xml is login
    /// so once initialization of this finished, login takes over futher actions
    /// the check of services goes with login, and job comes back to this model at a second step in row
    /// </summary>
    public class MainWindowViewModel : ViewModelBase
    {
        public MainWindowViewModel(IDialogCoordinator dialogCoordinator)
        {
            Title = "this is a title";
            //NavigateToView is declared in the model, 
            //could be set with a value at any sub views, and the MainWindow will execute a own function.
            this.NavigateToView = new SimpleCommand(
               o => true,
               x => ((MainWindow)Application.Current.MainWindow).Navigate_To_View(x.ToString())
               );
            Meetings = new ObservableCollection<Meeting>();
            Calls = new ObservableCollection<Call>();
            BaseContacts = new List<Contact>();
        }


        public bool UpdateData()
        {
            try
            {
                Departments = new ObservableCollection<Department>(
                DataService.GetInstance().Database.GetCollection<BsonDocument>("Department")
                    .Find(Builders<BsonDocument>.Filter.Empty)
                    .ToList()
                    .Select(x =>
                    {
                        Department _d = new Department
                        {
                            Id = (ObjectId)x["_id"],
                            Name = (string)x["Name"],
                            Rank = (int)x["Rank"],
                            Remark = (string)x["Remark"],
                            Father = (string)x["Father"]
                        };
                        return _d;
                    })

                );

                Positions = new ObservableCollection<Position>(
                    DataService.GetInstance().Database.GetCollection<BsonDocument>("Position")
                        .Find(Builders<BsonDocument>.Filter.Empty)
                        .ToList()
                        .Select(x =>
                        {
                            Position _p = new Position
                            {
                                Id = (ObjectId)x["_id"],
                                Name = (string)x["Name"],
                                Rank = (int)x["Rank"],
                                Remark = (string)x["Remark"]
                            };
                            return _p;
                        })

                    );
                BaseContacts = DataService.GetInstance().Database.GetCollection<BsonDocument>("Contact")
                        .Find(Builders<BsonDocument>.Filter.Empty)
                        .ToList()
                        .Select(x =>
                        {
                            Contact _c = new Contact
                            {
                                Id = (ObjectId)x["_id"],
                                Name = (string)x["Name"],
                                Rank = (int)x["Rank"],
                                IsFavorite = (bool)x["IsFavorite"]

                            };
                            BsonArray devices = (BsonArray)x["Devices"];
                            _c.Devices = new List<Device>();
                            foreach (BsonValue bv in devices)
                            {
                                Device device = new Device
                                {
                                    Number = (string)bv["Number"]
                                };
                                _c.Devices.Add(device);
                                DeviceType _deviceType = new DeviceType
                                {
                                    Id = (ObjectId)bv["DeviceType"]["_id"],
                                    Name = (string)bv["DeviceType"]["Name"]
                                };
                                device.DeviceType = _deviceType;
                            }

                            _c.Department = Departments.ToList().Find(y => y.Name.Equals((string)x["Department"]["Name"]));
                            _c.Position = Positions.ToList().Find(y => y.Name.Equals((string)x["Position"]["Name"]));
                            return _c;
                        })
                        .ToList();

                Groups = new ObservableCollection<Group>(DataService.GetInstance().QueryGroups(BaseContacts));
                Plans = DataService.GetInstance().QueryPlans(this);
                Meetings = DataService.GetInstance().QueryMeetings(this);
                AlarmInfos = DataService.GetInstance().Database.GetCollection<BsonDocument>("alarminfo")
                    .Find(Builders<BsonDocument>.Filter.Empty)
                    .ToList()
                    .Select(x =>
                    {
                        AlarmInfo alarmInfo = new AlarmInfo
                        {
                            Id = ((ObjectId)x["_id"]).ToString(),
                            Name = (string)x["name"],
                            Code = (string)x["code"],
                            Ivr = (string)x["ivr"],
                            CalleeNumber = (string)x["calleenumber"],
                            Token = (string)x["token"],
                            Remark = (string)x["remark"]
                        };
                        return alarmInfo;
                    })
                    .ToList();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
            }
            return false;
            
        }

        public ObservableCollection<Contact> Contacts { get; set; }


        //updated via events trigered by httpserver
        private ObservableCollection<Call> _calls;
        public ObservableCollection<Call> Calls
        {
            get
            {
                if (this._calls == null)
                {
                    _calls = new ObservableCollection<Call>();
                }
                return _calls;
            }
            set => this.Set(ref this._calls, value);
        }
        private ObservableCollection<Meeting> _meetings;
        public ObservableCollection<Meeting> Meetings
        {
            get
            {
                if (this._meetings == null)
                {
                    _meetings = new ObservableCollection<Meeting>();
                }
                return _meetings;
            }
            set => this.Set(ref this._meetings, value);
        }

        public ObservableCollection<Group> Groups { get; set; }
        public ObservableCollection<Plan> Plans { get; set; }
        public ObservableCollection<Department> Departments { get; set; }
        public ObservableCollection<Models.Position> Positions { get; set; }
        public List<Contact> BaseContacts { get; set; }
        public string Title { get; set; }
        private string _message;
        public string Message
        {
            get => this._message;
            set => this.Set(ref this._message, value);
        }


        private User _user;
        public User User
        {
            get => this._user;
            set => this.Set(ref this._user, value);
        }
        public string Version { get => "v1.1.0"; }

        public string TempUserName { get; set; }
        public bool TempThreadResult { get; set; }
        private Call _extno1Call;
        public Call ExtNoCall
        {
            get => this._extno1Call;
            set => this.Set(ref this._extno1Call, value);
        }
        private string _dialNo;
        public string DialNo
        {
            get => this._dialNo;
            set => this.Set(ref this._dialNo, value);
        }


        public Call ChosenCall { get; set; }
        private bool _isOnlyFavorite;
        public bool IsOnlyFavorite
        {
            get => this._isOnlyFavorite;
            set => this.Set(ref this._isOnlyFavorite, value);
        }

        private string _contactSearchText;
        public string ContactSearchText
        {
            get => this._contactSearchText;
            set => this.Set(ref this._contactSearchText, value);
        }
        public List<AlarmInfo> AlarmInfos { get; set; }

        public void NotifyChange([CallerMemberName] string propertyName = null)
        {
            base.OnPropertyChanged(propertyName);
        }

        public delegate void DelegateProcessAtControl(params object[] values);
        public DelegateProcessAtControl ProcessAtControl { get; set; }

        public async void PreprocessHttpEvent(JObject result)
        {
            string action = (string)result["action"];
            if (action.Equals("AlarmCall"))
            {
                string code = (string)result["code"];
                string token = (string)result["token"];
                AlarmInfo alarmInfo = AlarmInfos.Find(x => x.Code.Equals(code) && x.Token.Equals(token));
                if (alarmInfo != null)
                {
                    if (Convert.ToInt32(alarmInfo.CalleeNumber) >= Models.SystemConfig.Instance.ExtFrom &&
                    Convert.ToInt32(alarmInfo.CalleeNumber) <= Models.SystemConfig.Instance.ExtTo)
                    {
                        JObject detailResponse = await DataService.GetInstance()
                            .PostAsync("{\"ivrid\": \""
                            + alarmInfo.Ivr + "\",\"extid\": \""
                            + alarmInfo.CalleeNumber + "\",\"autoanswer\": \"no\"}", "/ivr/dial_extension", true);
                        Console.WriteLine(detailResponse.ToString());
                    }
                    else
                    {
                        JObject detailResponse = await DataService.GetInstance()
                            .PostAsync("{\"ivrid\": \""
                            + alarmInfo.Ivr + "\",\"outto\": \""
                            + alarmInfo.CalleeNumber + "\",\"fromext\": \""
                            + User.UserConfig.Extno1 + "\"}", "/ivr/dial_outbound", true);
                        Console.WriteLine(detailResponse.ToString());
                    }

                }
            }

            if (action.Equals("ExtensionStatus"))
            {
                string ext = (string)result["extension"];
                string status = (string)result["status"];
                ContactWithSingleDevice contactWithSingleDevice = ext.SetupContactStatus(status.ToAgentStatus(), this);
                ProcessAtControl?.Invoke(result, contactWithSingleDevice.Contact);
            }

            if (action.Equals("ANSWER"))
            {
                string callid = (string)result["callid"];
                Call existCall = (new List<Call>(Calls)).Find(c => c.CallId.Equals(callid));

                if (existCall == null)
                {
                    existCall = new Call();
                    existCall.CallId = callid;
                    existCall.Direction = "呼入";

                    JArray jArrayCall = (JArray)result["call"];
                    if (jArrayCall == null || jArrayCall.Count != 2)
                    {
                        return;
                    }
                    existCall.Callee = (string)jArrayCall[0]["ext"]["extid"];
                    existCall.CalleeChannelId = callid;
                    existCall.CalleeContact = existCall.Callee.SetupContact(this);
                    if (jArrayCall[1]["ext"] != null)
                    {
                        existCall.Caller = (string)jArrayCall[1]["ext"]["extid"];
                        existCall.CallerChannelId = callid;
                        existCall.CallerContact = existCall.Caller.SetupContact(this);
                    }
                    else
                    {
                        existCall.Caller = (string)jArrayCall[1]["inbound"]["from"];
                        existCall.CallerChannelId = (string)jArrayCall[1]["inbound"]["inboundid"];
                        existCall.CallerContact = existCall.Caller.SetupContact(this);
                    }
                    Calls.Add(existCall);
                    if (existCall.Caller.Equals(User.UserConfig.Extno1) || existCall.Callee.Equals(User.UserConfig.Extno1))
                    {
                        ExtNoCall = existCall;
                        ((MainWindow)Application.Current.MainWindow).SetupCallBarStatus("通话中");
                    }
                    NotifyChange("Calls");
                    ProcessAtControl?.Invoke(result);
                }
            }
            if (action.Equals("ANSWERED"))
            {
                string callid = (string)result["callid"];
                JArray jArrayCall = (JArray)result["call"];
                if (jArrayCall == null || jArrayCall.Count != 2)
                {
                    return;
                }
                Call existCall = (new List<Call>(Calls)).Find(c => c.CallId.Equals(callid));

                if (existCall == null)
                {
                    existCall = new Call();
                    existCall.CallId = callid;
                    existCall.Direction = "呼出";


                    existCall.Caller = (string)jArrayCall[0]["ext"]["extid"];
                    existCall.CallerChannelId = callid;
                    existCall.CallerContact = existCall.Caller.SetupContact(this);
                    if (jArrayCall[1]["ext"] != null)
                    {
                        existCall.Callee = (string)jArrayCall[1]["ext"]["extid"];
                        existCall.CalleeChannelId = callid;
                        existCall.CalleeContact = existCall.Callee.SetupContact(this);
                    }
                    else
                    {
                        existCall.Callee = (string)jArrayCall[1]["outbound"]["to"];
                        existCall.CalleeChannelId = (string)jArrayCall[1]["outbound"]["outboundid"];
                        existCall.CalleeContact = existCall.Callee.SetupContact(this);
                    }
                    Calls.Add(existCall);
                    if (existCall.Caller.Equals(User.UserConfig.Extno1) || existCall.Callee.Equals(User.UserConfig.Extno1))
                    {
                        ExtNoCall = existCall;
                        ((MainWindow)Application.Current.MainWindow).SetupCallBarStatus("通话中");
                    }
                    NotifyChange("Calls");
                    ProcessAtControl?.Invoke(result);
                }
            }

            if (action.Equals("BYE") || action.Equals("CallFailed"))
            {
                string callid = (string)result["callid"];
                Call existCall = (new List<Call>(Calls)).Find(c => c.CallId.Equals(callid));

                if (existCall != null)
                {

                    existCall.Caller.SetupContact(this);
                    existCall.Callee.SetupContact(this);
                    ProcessAtControl?.Invoke(result);
                    Calls.Remove(existCall);
                    if (existCall.Caller.Equals(User.UserConfig.Extno1) || existCall.Callee.Equals(User.UserConfig.Extno1))
                    {
                        ExtNoCall = null;
                        ((MainWindow)Application.Current.MainWindow).SetupCallBarStatus("空闲");
                    }
                    NotifyChange("Calls");
                }
                ProcessAtControl?.Invoke(result);
            }
        }

        private bool _loggedIn;

        public bool LoggedIn
        {
            get => this._loggedIn;
            set => this.Set(ref this._loggedIn, value);
        }
        public ICommand NavigateToView { get; }
        //Controls
        public UserControl CurrentUserControl { get; set; }

        public License License { get; set; }
        public Home Home { set; get; }
        public Login Login { get; set; }
        public Contacts ViewContacts { get; set; }
        public FavoriteContacts FavoriteContacts { get; set; }
        public History History { get; set; }
        public HistoryMeeting HistoryMeeting { get; set; }
        
        public Operations Operations { get; set; }
        public SingleCall SingleCall { get; set; }
        public Monitor Monitor { get; set; }
        public Groups ViewGroups { get; set; }
        public Incidents Incidents { get; set; }
        public Views.RollCall ViewRollCall { get; set; }

        public Users ViewUsers { get; set; }

        public Departments ViewDepartments { get; set; }

        public Views.SystemConfig SystemConfig { get; set; }

        public ViewPositions ViewPositions { get; set; }
        public AlarmCall AlarmCall { get; set; }

        public LoginConfig LoginConfig { get; set; }

        public UserPlans UserPlans { get; set; }

        public UserMeetings UserMeetings { get; set; }

        public HistoryCollection HistoryCollection { get; set; }
        public About About  { get; set; }
}
}
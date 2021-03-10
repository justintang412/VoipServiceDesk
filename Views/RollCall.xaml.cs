using Arco.Core;
using Arco.Models;
using Arco.Services;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Arco.Views
{
    /// <summary>
    /// RollCall.xaml 的交互逻辑
    /// </summary>
    public partial class RollCall : UserControl
    {
        private MainWindowViewModel _viewModel = null;
        public RollCall()
        {
            InitializeComponent();
        }
        public RollCallGroup NewRollCallGroup { get; set; }
        public RollCallGroup ChosenRollCallGroup { get; set; }
        public ContactWithSingleDevice ChosenContactWithSingleDevice { get; set; }
        public RollCallHistory NewRollCallHistory { get; set; }
        public ObservableCollection<RollCallHistory> MyRollCallHistories { get; set; }
        public RollCallHistory ChosenRollCallHistory { get; set; }
        public Arco.Models.RollCall ChosenRollCall { get; set; }
        public string ContactSearchText { get; set; }
        public bool IsOnlyFavorite { get; set; }
        public bool RecordingCalls { get; set; }
        public Contact ChosenContact { get; set; }
        public ObservableCollection<Contact> Contacts { get; set; }
        public ObservableCollection<RollCallGroup> MyRollCallGroups { get; set; }
        private CustomDialog CustomDialogAdd { get; set; }
        private void Control_Loaded(object sender, RoutedEventArgs e)
        {
            _viewModel = (DataContext as MainWindowViewModel);
            _viewModel.ViewRollCall = this;
            _viewModel.ProcessAtControl = this.ProcessAtControl;
            RecordingCalls = false;
            LoadData();
        }

        private void LoadData()
        {
            MyRollCallGroups = new ObservableCollection<RollCallGroup>(
                DataService.GetInstance().Database.GetCollection<BsonDocument>("rollcallgroup")
                .Find(new BsonDocument() { })
                .Sort("{rank: 1}")
                .ToList()
                .Select(x =>
                {
                    RollCallGroup rollCallGroup = new RollCallGroup();
                    rollCallGroup.Id = x["_id"].ToString();
                    rollCallGroup.Name = (string)x["name"];
                    rollCallGroup.Code = (string)x["code"];
                    rollCallGroup.Remark = (string)x["remark"];
                    rollCallGroup.Rank = (string)x["rank"];
                    rollCallGroup.Members = new ObservableCollection<ContactWithSingleDevice>();

                    BsonArray bsonArray = (BsonArray)x["members"];
                    if (bsonArray != null)
                    {
                        foreach (BsonValue bv in bsonArray)
                        {
                            ContactWithSingleDevice contactWithSingleDevice = new ContactWithSingleDevice();
                            foreach (Contact _contact in _viewModel.BaseContacts)
                            {
                                foreach (Device _device in _contact.Devices)
                                {
                                    if (_device.Number.Equals(bv.ToString()))
                                    {
                                        contactWithSingleDevice.Contact = _contact;
                                        contactWithSingleDevice.Device = _device;
                                        break;
                                    }
                                }
                            }


                            rollCallGroup.Members.Add(contactWithSingleDevice);
                        }
                    }

                    return rollCallGroup;
                }));

            LoadMyRollCallHistories();
        }

        private void LoadMyRollCallHistories()
        {
            MyRollCallHistories = new ObservableCollection<RollCallHistory>(
                 DataService.GetInstance().Database.GetCollection<BsonDocument>("rollcalls")
                 .Find(new BsonDocument() { })
                 .Sort(Builders<BsonDocument>.Sort.Descending("_id"))
                 .ToList()
                 .Select(x =>
                 {
                     RollCallHistory rollCallHistory = new RollCallHistory();
                     rollCallHistory.CallTime = (string)x["rollcalltime"];
                     RollCallGroup rollCallGroup = new RollCallGroup();
                     rollCallHistory.RollCallGroup = rollCallGroup;

                     rollCallGroup.Code = (string)x["group"];
                     BsonDocument groupDocument = DataService.GetInstance().Database.GetCollection<BsonDocument>("rollcallgroup")
                        .Find(new BsonDocument { { "code", rollCallGroup.Code } }).First();
                     rollCallGroup.Name = (string)groupDocument["name"];
                     rollCallGroup.Rank = (string)groupDocument["rank"];
                     rollCallGroup.Remark = (string)groupDocument["remark"];
                     rollCallGroup.Id = groupDocument["_id"].ToString();
                     rollCallGroup.Members = new ObservableCollection<ContactWithSingleDevice>();
                     BsonArray memberArray = (BsonArray)groupDocument["members"];
                     foreach (BsonValue bsonValue in memberArray)
                     {
                         rollCallGroup.Members.Add(bsonValue.ToString().ToContactWithSingleDevice(_viewModel));
                     }

                     BsonArray callArray = (BsonArray)x["calls"];
                     rollCallHistory.RollCalls = new ObservableCollection<Models.RollCall>();
                     foreach (BsonValue callBsonValue in callArray)
                     {
                         string callid = (string)callBsonValue["callid"];
                         string number = (string)callBsonValue["number"];
                         string result = (string)callBsonValue["result"];
                         Models.RollCall rollCall = new Models.RollCall();
                         rollCallHistory.RollCalls.Add(rollCall);
                         rollCall.CallHistory = callBsonValue.ToString().ToCallHitory();
                         rollCall.Result = result;
                         rollCall.ContactWithSingleDevice = number.ToContactWithSingleDevice(_viewModel);
                     }
                     return rollCallHistory;
                 }));
            _viewModel.NotifyChange("ViewRollCall");
            RollCallHistory_DataGrid.Items.Refresh();
            RollCallHistory_Members_Grid.Items.Refresh();
        }

        private void ProcessAtControl(params object[] values)
        {
            JObject result = values[0] as JObject;
            string action = (string)result["action"];
            string callid = (string)result["callid"];

            if (action.Equals("ANSWER") || action.Equals("ANSWERED"))
            {
                JArray jArray = (JArray)result["call"];
                string toNumber = null;
                foreach (JObject jo in jArray)
                {
                    if (jo["inbound"] != null)
                    {
                        toNumber = (string)jo["inbound"]["to"];
                        break;
                    }
                    if (jo["outbound"] != null)
                    {
                        toNumber = (string)jo["outbound"]["to"];
                        break;
                    }
                }
                if (toNumber == null)
                {
                    toNumber = (string)jArray[0]["ext"]["extid"];
                }
                if (toNumber == null) return;
                if (NewRollCallHistory == null || NewRollCallHistory.RollCalls == null) return;
                Models.RollCall rc = NewRollCallHistory.RollCalls.ToList().Find(x =>
                {
                    string xcallid = x.CallHistory?.Callid;
                    return xcallid != null && xcallid.Equals(callid);
                });
                if (rc == null || rc.Result != null || (!toNumber.Equals(rc.ContactWithSingleDevice.Device.Number))) return;
                rc.Result = "成功";
            }

            if (action.Equals("BYE") || action.Equals("CallFailed"))
            {
                JArray jArray = (JArray)result["call"];
                string toNumber = null;
                foreach (JObject jo in jArray)
                {
                    if (jo["inbound"] != null)
                    {
                        toNumber = (string)jo["inbound"]["to"];
                        break;
                    }
                    if (jo["outbound"] != null)
                    {
                        toNumber = (string)jo["outbound"]["to"];
                        break;
                    }
                }
                if (toNumber == null)
                {
                    toNumber = (string)jArray[0]["ext"]["extid"];
                }
                if (toNumber == null) return;

                if (toNumber == null) return;
                if (RecordingCalls && NewRollCallHistory != null &&
                    NewRollCallHistory.RollCalls != null)
                {
                    Models.RollCall rc = NewRollCallHistory.RollCalls.ToList().Find(x =>
                    {
                        string xcallid = x.CallHistory?.Callid;
                        return xcallid != null && xcallid.Equals(callid);
                    });
                    if (rc == null) return;
                    if (rc.Result != null && rc.Result.Equals("失败")) return;

                    if (!toNumber.Equals(rc.ContactWithSingleDevice.Device.Number)) return;
                    if (rc.Result != null && rc.Result.Equals("成功"))
                    {
                        StartOneNewCallOnPreviousEnded();
                    }
                    if (rc.Result == null)
                    {
                        rc.Result = "失败";
                        StartOneNewCallOnPreviousEnded();
                    }
                }
            }

            _viewModel.NotifyChange("ViewRollCall");
            RollCall_Members_Grid.Items.Refresh();
        }
        private void Control_Unloaded(object sender, RoutedEventArgs e)
        {
            _viewModel.ProcessAtControl = null;
            _viewModel.ViewRollCall = null;
        }

        private async void Add_Click(object sender, RoutedEventArgs e)
        {
            NewRollCallGroup = new RollCallGroup();
            NewRollCallGroup.Members = new ObservableCollection<ContactWithSingleDevice>();

            MainWindow mainWindow = Application.Current.MainWindow as MainWindow;

            if (CustomDialogAdd == null)
            {
                CustomDialogAdd = new CustomDialog(mainWindow.MetroDialogOptions)
                {
                    Content = this.Resources["CustomDialogAdd"],
                    DialogContentMargin = new GridLength(0),
                    Width = 600,
                    BorderThickness = new Thickness(0),
                    BorderBrush = Brushes.DarkGray,
                    Background = new SolidColorBrush(Color.FromRgb(16, 57, 77)),//#10394d
                    DialogTop = this.Resources["CustomDialogAddClose"]
                };
                Grid topGrid = CustomDialogAdd.DialogTop as Grid;
                topGrid.FindChild<Label>().SetCurrentValue(ContentProperty, "点名组");
                (topGrid.Children[2] as Button).Click += CustomDialogAddClose;
            }
            else
            {
                Grid topGrid = CustomDialogAdd.DialogTop as Grid;
                topGrid.FindChild<Label>().SetCurrentValue(ContentProperty, "点名组");
            }

            await mainWindow.ShowMetroDialogAsync(CustomDialogAdd);
        }
        private async void CustomDialogAddClose(object sender, RoutedEventArgs e)
        {
            await ((MainWindow)Application.Current.MainWindow).HideMetroDialogAsync(CustomDialogAdd);
        }


        private async void Edit_Click(object sender, RoutedEventArgs e)
        {
            if (ChosenRollCallGroup == null)
            {
                _viewModel.Message = "请选择一个点名组";
                return;
            }
            NewRollCallGroup = ChosenRollCallGroup;
            _viewModel.NotifyChange("ViewRollGroup");
            MainWindow mainWindow = Application.Current.MainWindow as MainWindow;

            if (CustomDialogAdd == null)
            {
                CustomDialogAdd = new CustomDialog(mainWindow.MetroDialogOptions)
                {
                    Content = this.Resources["CustomDialogAdd"],
                    DialogContentMargin = new GridLength(0),
                    Width = 600,
                    BorderThickness = new Thickness(0),
                    BorderBrush = Brushes.DarkGray,
                    Background = new SolidColorBrush(Color.FromRgb(16, 57, 77)),//#10394d
                    DialogTop = this.Resources["CustomDialogAddClose"]
                };
                Grid topGrid = CustomDialogAdd.DialogTop as Grid;
                topGrid.FindChild<Label>().SetCurrentValue(ContentProperty, "修改");
                (topGrid.Children[2] as Button).Click += CustomDialogAddClose;
            }
            else
            {
                Grid topGrid = CustomDialogAdd.DialogTop as Grid;
                topGrid.FindChild<Label>().SetCurrentValue(ContentProperty, "修改");
            }
            await mainWindow.ShowMetroDialogAsync(CustomDialogAdd);
        }

        private void Del_Click(object sender, RoutedEventArgs e)
        {
            if (ChosenRollCallGroup == null)
            {
                _viewModel.Message = "请选择一个点名组";
                return;
            }
            DataService.GetInstance().Database.GetCollection<BsonDocument>("rollcalls")
                .DeleteMany(new BsonDocument() { { "group", ChosenRollCallGroup.Code } });

            DataService.GetInstance().Database.GetCollection<BsonDocument>("rollcallgroup")
                .DeleteOne(new BsonDocument() { { "_id", new ObjectId(ChosenRollCallGroup.Id) } });

            _viewModel.Message = ChosenRollCallGroup.Name + "信息删除成功。";
            LoadData();
        }

        private void Contact_Search_Button(object sender, RoutedEventArgs e)
        {
            Contacts = new ObservableCollection<Contact>(new List<Contact>(_viewModel.BaseContacts).FindAll(x =>
            {
                if (this.IsOnlyFavorite)
                {
                    return x.IsFavorite && (ContactSearchText == null || x.Name.Contains(ContactSearchText) || x.Department.Name.Contains(ContactSearchText) || x.Position.Name.Contains(ContactSearchText) || x.Devices[0].Number.Contains(ContactSearchText) || x.Devices[1].Number.Contains(ContactSearchText));
                }
                else
                {
                    return ContactSearchText == null || x.Name.Contains(ContactSearchText) || x.Department.Name.Contains(ContactSearchText) || x.Position.Name.Contains(ContactSearchText) || x.Devices[0].Number.Contains(ContactSearchText) || x.Devices[1].Number.Contains(ContactSearchText);
                }
            }));
            _viewModel.NotifyChange("ViewRollCall");
        }


        private async void Record_Click(object sender, RoutedEventArgs e)
        {
            if (ChosenRollCall == null) return;

            (sender as Button).SetCurrentValue(IsEnabledProperty, false);
            CallHistory _callhistory = ChosenRollCall.CallHistory;

            string json = "{\"recording\": \"" + _callhistory.Recording + "\"}";
            JObject response = await DataService.GetInstance().PostAsync(json, "/recording/get_random", true);
            if (response != null)
            {


                if (((string)response["status"]).Equals("Success"))
                {

                    string random = (string)response["random"];
                    string recording = (string)response["recording"];
                    if (recording.Length > 0)
                    {
                        if (!File.Exists(DataService.GetInstance().DataFolder + recording))
                        {
                            string uri = "/recording/download?recording=" + recording + "&token=" + DataService.GetInstance().Token + "&random=" + random;
                            await DataService.GetInstance().DownloadRecording(uri, recording);

                        }
                        await Task.Run(() =>
                        {
                            System.Media.SoundPlayer player =
                                new System.Media.SoundPlayer(DataService.GetInstance().DataFolder + recording);
                            player.Play();
                        });
                    }
                    

                }
            }

            (sender as Button).SetCurrentValue(IsEnabledProperty, true);
        }

        private void RollCallGroup_DataGrid_Seletion_Changed(object sender, SelectionChangedEventArgs e)
        {
            ChosenRollCallGroup = (sender as DataGrid).SelectedItem as RollCallGroup;
            _viewModel.NotifyChange("ViewRollCall");
            //RollCall_Members_Grid.Items.Refresh();
        }

        private void RollCallHistory_DataGrid_Seletion_Changed(object sender, SelectionChangedEventArgs e)
        {
            ChosenRollCallHistory = (sender as DataGrid).SelectedItem as RollCallHistory;
            _viewModel.NotifyChange("ViewRollCall");
            RollCallHistory_Members_Grid.Items.Refresh();
        }

        private void RollCallHistory_Members_Grid_Changed(object sender, SelectionChangedEventArgs e)
        {
            ChosenRollCall = (sender as DataGrid).SelectedItem as Arco.Models.RollCall;

        }

        private void SaveRollCallGroup(RollCallGroup rollCallGroup)
        {
            
            if (rollCallGroup == null||
                rollCallGroup.Code==null||
                rollCallGroup.Name==null||
                rollCallGroup.Rank==null) return;
            _viewModel.Message = "正在保存组信息...";
            if (rollCallGroup.Id != null)
            {
                FilterDefinition<BsonDocument> filter = Builders<BsonDocument>.Filter.Eq("_id", new ObjectId(rollCallGroup.Id));
                UpdateDefinition<BsonDocument> update = Builders<BsonDocument>.Update
                    .Set("code", rollCallGroup.Code ?? "")
                    .Set("name", rollCallGroup.Name)
                    .Set("rank", rollCallGroup.Rank)
                    .Set("remark", rollCallGroup.Remark??"");
                BsonArray members = new BsonArray();
                if (rollCallGroup.Members != null && rollCallGroup.Members.Count() > 0)
                {
                    foreach (ContactWithSingleDevice contactWithSingleDevice in rollCallGroup.Members)
                    {
                        members.Add(contactWithSingleDevice.Device.Number);
                    }
                }
                update = update.Set("members", members);
                DataService.GetInstance().Database.GetCollection<BsonDocument>("rollcallgroup").UpdateOne(filter, update);
            }
            else
            {
                BsonArray members = new BsonArray();
                if (rollCallGroup.Members != null && rollCallGroup.Members.Count() > 0)
                {
                    foreach (ContactWithSingleDevice contactWithSingleDevice in rollCallGroup.Members)
                    {
                        members.Add(contactWithSingleDevice.Device.Number);
                    }
                }
                BsonDocument bsonDocument = new BsonDocument {
                    {"code",NewRollCallGroup.Code??""},
                    {"name",NewRollCallGroup.Name},
                    {"rank",NewRollCallGroup.Rank},
                    {"remark",  NewRollCallGroup.Remark??""},
                    {"members", members }
                };
                DataService.GetInstance().Database.GetCollection<BsonDocument>("rollcallgroup").InsertOne(bsonDocument);
            }

            _viewModel.NotifyChange("ViewRollCall");
            _viewModel.Message += "成功";
            RollCallGroup_DataGrid.Items.Refresh();
            RollCall_Members_Grid.Items.Refresh();
        }

        private async void Save_RollCallGroup(object sender, RoutedEventArgs e)
        {
            if (NewRollCallGroup == null) return;
            SaveRollCallGroup(NewRollCallGroup);

            await ((MainWindow)Application.Current.MainWindow).HideMetroDialogAsync(CustomDialogAdd);
            LoadData();
        }

        private void Add_Member_Click(object sender, RoutedEventArgs e)
        {
            if (ChosenContact == null || ChosenRollCallGroup == null) return;
            if (ChosenRollCallGroup.Members == null)
            {
                ChosenRollCallGroup.Members = new ObservableCollection<ContactWithSingleDevice>();
            }
            ContactWithSingleDevice _c = new List<ContactWithSingleDevice>(ChosenRollCallGroup.Members).Find(
                x => x.Device.Number.Equals(ChosenContact.Devices[0].Number)
                );
            if (_c != null) return;
            ChosenContact.Status = "ready".ToCallStatus();
            ChosenContact.Devices[0].Status = ChosenContact.Status;

            ChosenRollCallGroup.Members.Add(new ContactWithSingleDevice
            {
                Contact = ChosenContact,
                Device = ChosenContact.Devices[0]
            });
            SaveRollCallGroup(ChosenRollCallGroup);
        }

        private void Remove_Member_Click(object sender, RoutedEventArgs e)
        {
            if (ChosenContactWithSingleDevice == null || ChosenRollCallGroup == null) return;
            if (ChosenRollCallGroup.Members == null) return;

            ChosenRollCallGroup.Members.Remove(ChosenContactWithSingleDevice);
            SaveRollCallGroup(ChosenRollCallGroup);
        }

        private void RollCall_Members_Grid_Changed(object sender, SelectionChangedEventArgs e)
        {
            ChosenContactWithSingleDevice = (sender as DataGrid).SelectedItem as ContactWithSingleDevice;
        }

        private async void NewRollCallHistorySaveAndStop()
        {
            RecordingCalls = false;
            ProcessingNumbers = new List<string>();
            BsonArray rollcalls = new BsonArray();
            foreach (Models.RollCall rollcall in NewRollCallHistory.RollCalls)
            {
                string number = rollcall.ContactWithSingleDevice.Device.Number;
                string result = rollcall.Result;
                string callid = rollcall.CallHistory?.Callid;
                if (callid == null) callid = "-1";
                rollcalls.Add(new BsonDocument { { "number", number }, { "result", result }, { "callid", callid } });
            }
            await DataService.GetInstance().Database.GetCollection<BsonDocument>("rollcalls").InsertOneAsync(new BsonDocument {
                    {"rollcalltime",NewRollCallHistory.CallTime },
                    {"group",NewRollCallHistory.RollCallGroup.Code },
                    {"calls", rollcalls }
                });

            Roll_Call_Button.SetCurrentValue(IsEnabledProperty, true);
            NewRollCallHistory = null;
            _viewModel.Message = "点名呼叫完成。";
            RollCall_Members_Grid.Items.Refresh();
            LoadMyRollCallHistories();
        }

        List<string> ProcessingNumbers { get; set; }
        private async void StartOneNewCallOnPreviousEnded()
        {
            if (NewRollCallHistory == null || NewRollCallHistory.RollCalls == null) return;
            if (!RecordingCalls) return;
            //everybody is full            

            ContactWithSingleDevice contactWithSingleDevice = NewRollCallHistory.RollCallGroup.Members.ToList().Find(x =>
            {
                return NewRollCallHistory.RollCalls.ToList().Find(y => y.ContactWithSingleDevice.Contact.Name.Equals(x.Contact.Name)) == null;
            });
            if (contactWithSingleDevice != null)
            {
                lock (this)
                {
                    if (ProcessingNumbers.Contains(contactWithSingleDevice.Device.Number))
                    {
                        return;
                    }
                    else
                    {
                        ProcessingNumbers.Add(contactWithSingleDevice.Device.Number);
                    }
                }
            }
            
            if (contactWithSingleDevice == null)
            {
                NewRollCallHistorySaveAndStop();
                return;
            }

            string calleenumber = contactWithSingleDevice.Device.Number;
            if (calleenumber == null || calleenumber.Length == 0) return;


            string uri;
            string json;
            if (Convert.ToInt32(calleenumber) >=Models.SystemConfig.Instance.ExtFrom && Convert.ToInt32(calleenumber) <= Models.SystemConfig.Instance.ExtTo)
            {
                uri = "/extension/dial_extension";
                json = "{" +
                    "\"caller\": \"" + _viewModel.User.UserConfig.Extno1 + "\"," +
                    "\"callee\": \"" + calleenumber + "\"," +
                    "\"autoanswer\": \"no\"" +
                    "}";
            }
            else
            {
                uri = "/extension/dial_outbound";
                json = "{" +
                    "\"extid\": \"" + _viewModel.User.UserConfig.Extno1 + "\"," +
                    "\"outto\": \"" + calleenumber + "\"," +
                    "\"autoanswer\": \"no\"" +
                    "}";
            }


            _viewModel.Message = "正在呼叫" + contactWithSingleDevice.Contact.Name + "...";

            JObject response = await DataService.GetInstance().PostAsync(json, uri, true);

            if (response != null && ((string)response["status"]).Equals("Success"))
            {
                string callid = (string)response["callid"];
                Models.RollCall rc = new Models.RollCall
                {
                    CallHistory = new CallHistory { Callid = callid },
                    ContactWithSingleDevice = contactWithSingleDevice
                };
                NewRollCallHistory.RollCalls.Add(rc);

                _viewModel.NotifyChange("ViewRollCall");
                RollCall_Members_Grid.Items.Refresh();

                await Task.Run(() =>
                {
                    int c = 0;
                    //Not anwsered or rejected in 15 seconds leads to fail and ignored
                    //Not exist phone number goes to waiting time with the phone
                    while (rc.Result == null && c < 15)
                    {
                        Thread.Sleep(1000);
                        c++;
                    }
                });
                if (rc.Result == null)
                {
                    rc.Result = "失败";
                    StartOneNewCallOnPreviousEnded();
                }
            }
            else
            {
                Models.RollCall rollCall = new Models.RollCall
                {
                    ContactWithSingleDevice = contactWithSingleDevice,
                    Result = "失败"
                };
                NewRollCallHistory.RollCalls.Add(rollCall);
                _viewModel.Message += "失败";
                _viewModel.NotifyChange("ViewRollCall");
                RollCall_Members_Grid.Items.Refresh();
                StartOneNewCallOnPreviousEnded();
            }

        }

        private void Roll_Call_Click(object sender, RoutedEventArgs e)
        {
            if (RollCallGroup_DataGrid.SelectedItem == null) return;
            ChosenRollCallGroup = RollCallGroup_DataGrid.SelectedItem as RollCallGroup;
            if (ChosenRollCallGroup.Members == null || ChosenRollCallGroup.Members.Count == 0) return;
            RecordingCalls = true;
            _viewModel.Message = "正在进行点名呼叫...";
            Roll_Call_Button.SetCurrentValue(IsEnabledProperty, false);
            NewRollCallHistory = new RollCallHistory();
            NewRollCallHistory.RollCallGroup = ChosenRollCallGroup;
            NewRollCallHistory.RollCalls = new ObservableCollection<Models.RollCall>();
            NewRollCallHistory.CallTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            ProcessingNumbers = new List<string>();

            StartOneNewCallOnPreviousEnded();
        }

        private void Contact_Grid_Selection_Changed(object sender, SelectionChangedEventArgs e)
        {
            ChosenContact = (sender as DataGrid).SelectedItem as Contact;
        }
    }
}

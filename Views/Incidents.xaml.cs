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
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Arco.Views
{
    /// <summary>
    /// Incidents.xaml 的交互逻辑
    /// </summary>
    public partial class Incidents : UserControl
    {
        private MainWindowViewModel _viewModel = null;
        public Incidents()
        {
            InitializeComponent();
            CallHistory = new ObservableCollection<CallHistory>();
            TotalPages = 0;
            Page = 1;
        }

        private async Task LoadData()
        {
            IMongoDatabase mongoDatabase = DataService.GetInstance().Database;
            FilterDefinitionBuilder<BsonDocument> builder = Builders<BsonDocument>.Filter;
            FilterDefinition<BsonDocument> filter = builder.Empty;
            if (SearchText != null && SearchText.Length > 0)
            {
                filter = builder.And(filter, builder.Or(builder.Regex("callfrom", SearchText), builder.Regex("callto", SearchText)));
            }

            SortDefinition<BsonDocument> sort = Builders<BsonDocument>.Sort.Descending("timestart");
            long totalCount = await mongoDatabase.GetCollection<BsonDocument>("history").CountDocumentsAsync(filter);
            if (totalCount % 500 > 0)
            {
                TotalPages = Convert.ToInt32(totalCount / 500 + 1);
            }
            else
            {
                TotalPages = Convert.ToInt32(totalCount / 500);
            }

            CallHistory = await Task.Run(() =>
            {
                IEnumerable<CallHistory> _history = mongoDatabase.GetCollection<BsonDocument>("history").Find(filter)
                 .Sort(new BsonDocument("_id", -1))
                 .Skip(500 * (Page - 1))
                 .Limit(500)
                 .ToList()
                 .Select(x =>
                 {
                     CallHistory callHistory = new CallHistory()
                     {
                         Callid = (string)x["callid"],
                         Timestart = ((string)x["timestart"]),
                         Callfrom = (string)x["callfrom"],
                         Callto = (string)x["callto"],
                         Callduraction = (string)x["callduraction"],
                         Talkduraction = (string)x["talkduraction"],
                         Status = (string)x["status"],
                         Type = (string)x["type"],
                         Recording = (string)x["recording"],
                         Sn = (string)x["sn"]
                     };
                     return callHistory;
                 });
                return new ObservableCollection<CallHistory>(_history);
            });
            _viewModel.NotifyChange("Incidents");
        }
        public ObservableCollection<CallHistory> CallHistory { get; set; }
        private void History_Search_Button(object sender, RoutedEventArgs e)
        {
            LoadData();
        }

        public int TotalPages { get; set; }
        public int Page { get; set; }
        public string SearchText { get; set; }
        public Incident NewIncident { get; set; }
        public Incident ChosenIncident { get; set; }
        public CallHistory ChosenCallHistory { get; set; }
        public string ContactSearchText { get; set; }
        public bool IsOnlyFavorite { get; set; }

        public Contact ChosenContact { get; set; }
        public ObservableCollection<Contact> Contacts { get; set; }
        public ObservableCollection<Incident> MyIncidents { get; set; }
        public bool RecordingCalls { get; set; }
        private CustomDialog CustomDialogAdd { get; set; }
        private void Control_Loaded(object sender, RoutedEventArgs e)
        {
            _viewModel = (DataContext as MainWindowViewModel);
            _viewModel.Incidents = this;
            _viewModel.ProcessAtControl = null;
            RecordingCalls = false;
            loadIncidents();
        }


        private void loadIncidents()
        {
            MyIncidents = new ObservableCollection<Incident>(
                DataService.GetInstance().Database.GetCollection<BsonDocument>("incidents")
                .Find(new BsonDocument() { })
                .Sort("{_id: -1}")
                .ToList()
                .Select(x =>
                {
                    Incident incident = new Incident();
                    incident.Id = x["_id"].ToString();
                    incident.Type = (string)x["type"];
                    incident.ReportTime = (string)x["reporttime"];
                    incident.ReportNumber = (string)x["reportnumber"];
                    incident.ReportContent = (string)x["reportcontent"];
                    incident.CallHistories = new ObservableCollection<CallHistory>();

                    BsonArray bsonArray = (BsonArray)x["calls"];
                    if (bsonArray != null)
                    {
                        foreach (BsonValue bv in bsonArray)
                        {
                            incident.CallHistories.Add(bv.ToString().ToCallHitory());
                        }
                    }

                    return incident;
                }));
            _viewModel.NotifyChange("Incidents");
        }

        private void Control_Unloaded(object sender, RoutedEventArgs e)
        {
            _viewModel.ProcessAtControl = null;
            _viewModel.Incidents = null;
        }

        private async void Add_Click(object sender, RoutedEventArgs e)
        {
            NewIncident = new Incident();
            
            NewIncident.ReportTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            NewIncident.CallHistories = new ObservableCollection<CallHistory>();

            MainWindow mainWindow = Application.Current.MainWindow as MainWindow;

            if (CustomDialogAdd == null)
            {
                CustomDialogAdd = new CustomDialog(mainWindow.MetroDialogOptions)
                {
                    Content = this.Resources["CustomDialogAdd"],
                    DialogContentMargin = new GridLength(0),
                    Width = 650,
                    BorderThickness = new Thickness(0),
                    BorderBrush = Brushes.DarkGray,
                    Background = new SolidColorBrush(Color.FromRgb(16, 57, 77)),//#10394d
                    DialogTop = this.Resources["CustomDialogAddClose"]
                };
                Grid topGrid = CustomDialogAdd.DialogTop as Grid;
                topGrid.FindChild<Label>().SetCurrentValue(ContentProperty, "事件信息");
                (topGrid.Children[2] as Button).Click += CustomDialogAddClose;
            }
            else
            {
                Grid topGrid = CustomDialogAdd.DialogTop as Grid;
                topGrid.FindChild<Label>().SetCurrentValue(ContentProperty, "事件信息");
            }
            await mainWindow.ShowMetroDialogAsync(CustomDialogAdd);
        }
        private async void CustomDialogAddClose(object sender, RoutedEventArgs e)
        {
            await ((MainWindow)Application.Current.MainWindow).HideMetroDialogAsync(CustomDialogAdd);
        }

        private void SaveIncidentInfo()
        {
            if (NewIncident == null || NewIncident.ReportContent == null ||
                NewIncident.ReportNumber == null ||
                NewIncident.ReportTime == null) return;
            if (NewIncident.Id != null)
            {
                FilterDefinition<BsonDocument> filter = Builders<BsonDocument>.Filter.Eq("_id", new ObjectId(NewIncident.Id));
               
                BsonArray calls = new BsonArray();
                if (NewIncident.CallHistories != null && NewIncident.CallHistories.Count() > 0)
                {
                    foreach (CallHistory callHistory in NewIncident.CallHistories)
                    {
                        calls.Add(callHistory.Callid);
                    }
                }
                UpdateDefinition<BsonDocument> update = Builders<BsonDocument>.Update
                   .Set("reporttime", NewIncident.ReportTime??"")
                   .Set("type", NewIncident.Type??"")
                   .Set("reportnumber", NewIncident.ReportNumber??"")
                   .Set("reportcontent", NewIncident.ReportContent??"")
                   .Set("calls", calls);
                DataService.GetInstance().Database.GetCollection<BsonDocument>("incidents").UpdateOne(filter, update);
            }
            else
            {
                BsonArray calls = new BsonArray();
                if (NewIncident.CallHistories != null && NewIncident.CallHistories.Count() > 0)
                {
                    foreach (CallHistory callHistory in NewIncident.CallHistories)
                    {
                        calls.Add(callHistory.Callid);
                    }
                }
                BsonDocument bsonDocument = new BsonDocument {
                    {"reporttime",NewIncident.ReportTime},
                    {"type",NewIncident.Type},
                    {"reportnumber",NewIncident.ReportNumber},
                    {"reportcontent",  NewIncident.ReportContent},
                    {"calls", calls }
                };
                DataService.GetInstance().Database.GetCollection<BsonDocument>("incidents").InsertOne(bsonDocument);
            }
        }

        private async void Save_Incident(object sender, RoutedEventArgs e)
        {
            _viewModel.Message = "正在保存事件信息...";

            SaveIncidentInfo();
            await ((MainWindow)Application.Current.MainWindow).HideMetroDialogAsync(CustomDialogAdd);

            loadIncidents();
            _viewModel.Message += "成功";
        }
        private void Pre_Page(object sender, RoutedEventArgs e)
        {
            if (Page > 1)
            {
                Page = Page - 1;
            }
            LoadData();
        }

        private void Next_Page(object sender, RoutedEventArgs e)
        {
            if (Page < TotalPages)
            {
                Page = Page + 1;
            }
            LoadData();
        }

        private void Go_Page(object sender, RoutedEventArgs e)
        {
            if (Page <= TotalPages && Page >= 1)
            {
                LoadData();
            }
        }
        private async void Listen_Click(object sender, RoutedEventArgs e)
        {
            if (Cdr_Grid.SelectedItem == null) return;
            (sender as Button).SetCurrentValue(IsEnabledProperty, false);
            CallHistory _callhistory = Cdr_Grid.SelectedItem as CallHistory;

            string json = "{\"recording\": \"" + _callhistory.Recording + "\"}";
            JObject response = await DataService.GetInstance().PostAsync(json, "/recording/get_random", true);
            if (response != null)
            {
                if (((string)response["status"]).Equals("Success"))
                {

                    string random = (string)response["random"];
                    string recording = (string)response["recording"];
                    if (recording == null || recording.Length == 0)
                    {
                        (sender as Button).SetCurrentValue(IsEnabledProperty, true);
                        return;
                    }
                    if (!File.Exists(DataService.GetInstance().DataFolder + recording))
                    {
                        try
                        {
                            string uri = "/recording/download?recording=" + recording + "&token=" + DataService.GetInstance().Token + "&random=" + random;
                            await DataService.GetInstance().DownloadRecording(uri, recording);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.StackTrace);
                        }
                    }

                    if (File.Exists(DataService.GetInstance().DataFolder + recording))
                    {
                        await Task.Run(() => {
                            System.Media.SoundPlayer player =
                                new System.Media.SoundPlayer(DataService.GetInstance().DataFolder + recording);
                            player.Play();
                        });
                    }
                }
            }

            (sender as Button).SetCurrentValue(IsEnabledProperty, true);
        }

        private async void Edit_Click(object sender, RoutedEventArgs e)
        {
            if (ChosenIncident == null)
            {
                _viewModel.Message = "请选择一个事件";
                return;
            }
            NewIncident = ChosenIncident;

            _viewModel.NotifyChange("Incidents");
            MainWindow mainWindow = Application.Current.MainWindow as MainWindow;

            if (CustomDialogAdd == null)
            {
                CustomDialogAdd = new CustomDialog(mainWindow.MetroDialogOptions)
                {
                    Content = this.Resources["CustomDialogAdd"],
                    DialogContentMargin = new GridLength(0),
                    Width = 650,
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
            if (ChosenIncident == null)
            {
                _viewModel.Message = "请选择一个事件";
                return;
            }
            FilterDefinition<BsonDocument> filter = Builders<BsonDocument>.Filter.Eq("_id", new ObjectId(ChosenIncident.Id));
            IMongoCollection<BsonDocument> mongoColleciton = DataService.GetInstance().Database.GetCollection<BsonDocument>("incidents");
            mongoColleciton.DeleteOne(filter);
            _viewModel.Message = ChosenIncident.ReportNumber + "信息删除成功。";
            MyIncidents.Remove(ChosenIncident);
            ChosenIncident = null;
            _viewModel.NotifyChange("Incidents");
        }

        private void Incidents_DataGrid_Seletion_Changed(object sender, SelectionChangedEventArgs e)
        {
            ChosenIncident = (sender as DataGrid).SelectedItem as Incident;
            _viewModel.NotifyChange("Incidents");
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
            _viewModel.NotifyChange("Incidents");
        }

        private void Device_Grid_Changed(object sender, SelectionChangedEventArgs e)
        {
            _viewModel.DialNo = ((sender as DataGrid).SelectedItem as Device)?.Number;
        }

        private async void Record_Click(object sender, RoutedEventArgs e)
        {
            if (Call_History_Grid.SelectedItem == null) return;
            (sender as Button).SetCurrentValue(IsEnabledProperty, false);
            CallHistory _callhistory = Call_History_Grid.SelectedItem as CallHistory;

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

        private void Src_Cdr_Double_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender != null && sender is DataGrid)
            {
                DataGrid dataGrid = sender as DataGrid;
                if (dataGrid.SelectedItem != null && dataGrid.SelectedItem is CallHistory && ChosenIncident != null)
                {
                    if (ChosenIncident.CallHistories == null)
                    {
                        ChosenIncident.CallHistories = new ObservableCollection<CallHistory>();

                    }
                    CallHistory history = ChosenIncident.CallHistories.ToList().Find(x => x.Callid.Equals((dataGrid.SelectedItem as CallHistory).Callid));
                    if (history == null)
                    {
                        ChosenIncident.CallHistories.Add(dataGrid.SelectedItem as CallHistory);
                        NewIncident = ChosenIncident;
                        SaveIncidentInfo();
                        _viewModel.NotifyChange("Incidents");
                    }
                }
            }
        }

        private void Dest_Cdr_Double_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender != null && sender is DataGrid)
            {
                DataGrid dataGrid = sender as DataGrid;
                if (dataGrid.SelectedItem != null && dataGrid.SelectedItem is CallHistory && ChosenIncident != null)
                {
                    if (ChosenIncident.CallHistories == null)
                    {
                        ChosenIncident.CallHistories = new ObservableCollection<CallHistory>();

                    }
                    CallHistory history = ChosenIncident.CallHistories.ToList().Find(x => x.Callid.Equals((dataGrid.SelectedItem as CallHistory).Callid));
                    if (history != null)
                    {
                        ChosenIncident.CallHistories.Remove(dataGrid.SelectedItem as CallHistory);
                        NewIncident = ChosenIncident;
                        SaveIncidentInfo();
                        _viewModel.NotifyChange("Incidents");
                    }
                }
            }
        }
    }
}

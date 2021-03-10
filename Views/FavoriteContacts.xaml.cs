using Arco.Models;
using Arco.Services;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Arco.Views
{
    /// <summary>
    /// FavoriteContacts.xaml 的交互逻辑
    /// </summary>
    public partial class FavoriteContacts : UserControl
    {
        private MainWindowViewModel _viewModel = null;
        public FavoriteContacts()
        {
            InitializeComponent();
            Contacts = new ObservableCollection<Contact>();
        }
        public string ContactSearchText { get; set; }
        public ObservableCollection<Contact> Contacts { get; set; }
        public ObservableCollection<CallHistory> CallHistory { get; set; }
        public Contact ChosenContact { get; set; }
        private async void Contact_Search_Button(object sender, RoutedEventArgs e)
        {
            await Task.Run(()=>
            {
                LoadData();
            });
        }
        private void LoadData()
        {
            _viewModel.BaseContacts = DataService.GetInstance().Database.
               GetCollection<Contact>("Contact").Find(new BsonDocument { }).ToList();
            _viewModel.Departments = new ObservableCollection<Department>();
            new List<Contact>(_viewModel.BaseContacts).ForEach(x => {
                if (new List<Department>(_viewModel.Departments).Find(y => y.Name.Equals(x.Department.Name)) == null)
                {
                    _viewModel.Departments.Add(x.Department);
                }
            });
            Contacts = new ObservableCollection<Contact>(new List<Contact>(_viewModel.BaseContacts).FindAll(x =>
            {
                return x.IsFavorite && (ContactSearchText == null || x.Name.Contains(ContactSearchText) || x.Department.Name.Contains(ContactSearchText) || x.Position.Name.Contains(ContactSearchText) || x.Devices[0].Number.Contains(ContactSearchText) || x.Devices[1].Number.Contains(ContactSearchText));
            }));
            _viewModel.NotifyChange("FavoriteContacts");
        }
        private void ContactDeviceListSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _viewModel.DialNo = ((sender as DataGrid).SelectedItem as Device)?.Number;
        }

        private void Favorite_Contacts_Loaded(object sender, RoutedEventArgs e)
        {
            
            _viewModel = (DataContext as MainWindowViewModel);
            _viewModel.FavoriteContacts = this;
            LoadData();
        }

        private void Control_Unloaded(object sender, RoutedEventArgs e)
        {
            _viewModel.ProcessAtControl = null;
            _viewModel.FavoriteContacts = null;
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

        private async void ContactGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ChosenContact = ((sender as DataGrid).SelectedItem as Contact);
            if (ChosenContact == null) return;
            FilterDefinition<BsonDocument> filter = null;
            foreach (Device device in ChosenContact.Devices)
            {
                if (filter != null)
                {
                    filter = Builders<BsonDocument>.Filter.Or(filter,
                    Builders<BsonDocument>.Filter.Eq("callfrom", device.Number),
                    Builders<BsonDocument>.Filter.Eq("callto", device.Number)
                    );
                }
                else
                {
                    filter = Builders<BsonDocument>.Filter.Or(
                    Builders<BsonDocument>.Filter.Eq("callfrom", device.Number),
                    Builders<BsonDocument>.Filter.Eq("callto", device.Number)
                    );
                }

            }
            ObservableCollection<CallHistory> _callhistory = null;

            await Task.Run(() =>
            {
                _callhistory = new ObservableCollection<CallHistory>(
            DataService.GetInstance().Database.GetCollection<BsonDocument>("history")
                .Find(filter)
                .Sort("{_id: -1}")
                .Limit(50)
                .ToList()
                .Select(
                    x =>
                    {
                        CallHistory callHistory = new CallHistory
                        {
                            Callid = (string)x["callid"],
                            Timestart = (string)x["timestart"],
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
                    }

                )
                );
            });

            this.CallHistory = _callhistory;
            _viewModel.NotifyChange("FavoriteContacts");
        }
    }
}

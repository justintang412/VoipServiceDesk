using Arco.Models;
using Arco.Services;
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

namespace Arco.Views
{
    /// <summary>
    /// HistoryCollection.xaml 的交互逻辑
    /// </summary>
    public partial class HistoryCollection : UserControl
    {
        private MainWindowViewModel _viewModel = null;
        public HistoryCollection()
        {
            InitializeComponent();
            CallHistory = new ObservableCollection<CallHistory>();
            TotalPages = 0;
            Page = 1;
        }
        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            _viewModel = (DataContext as MainWindowViewModel);
            _viewModel.HistoryCollection = this;
            LoadData();
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
            long totalCount = await mongoDatabase.GetCollection<BsonDocument>("historycollection").CountDocumentsAsync(filter);
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
                IEnumerable<CallHistory> _history = mongoDatabase.GetCollection<BsonDocument>("historycollection").Find(filter)
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
            _viewModel.NotifyChange("HistoryCollection");
        }
        public ObservableCollection<CallHistory> CallHistory { get; set; }

        public int TotalPages { get; set; }
        public int Page { get; set; }
        public string SearchText { get; set; }
        public bool Inbound { get; set; }
        public bool Outbound { get; set; }

        private void History_Search_Button(object sender, RoutedEventArgs e)
        {
            LoadData();
        }



        private void Download_Click(object sender, RoutedEventArgs e)
        {

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


        private void Control_Unloaded(object sender, RoutedEventArgs e)
        {
            _viewModel.ProcessAtControl = null;
            _viewModel.HistoryCollection = null;
        }
    }
}

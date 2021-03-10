using Arco.Models;
using Arco.Services;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Arco.Views
{
    /// <summary>
    /// Operations.xaml 的交互逻辑
    /// </summary>
    public partial class Operations : UserControl
    {
        private MainWindowViewModel _viewModel = null;
        public Operations()
        {
            InitializeComponent();
            Logs = new ObservableCollection<Log>();
            TotalPages = 0;
            Page = 1;
        }

        private void Control_Loaded(object sender, RoutedEventArgs e)
        {
            _viewModel = (DataContext as MainWindowViewModel);
            _viewModel.Operations = this;
            LoadData();
        }
        private async Task LoadData()
        {
            IMongoDatabase mongoDatabase = DataService.GetInstance().Database;
            FilterDefinitionBuilder<BsonDocument> builder = Builders<BsonDocument>.Filter;
            FilterDefinition<BsonDocument> filter = builder.Empty;
            if (SearchText != null && SearchText.Length > 0)
            {
                filter = builder.And(filter, builder.Or(
                    builder.Regex("user", SearchText), 
                    builder.Regex("module", SearchText), 
                    builder.Regex("menu", SearchText),
                    builder.Regex("description", SearchText),
                    builder.Regex("action", SearchText)
                    ));
            }
            
            SortDefinition<BsonDocument> sort = Builders<BsonDocument>.Sort.Descending("_id");
            long totalCount = await mongoDatabase.GetCollection<BsonDocument>("log").CountDocumentsAsync(filter);
            if (totalCount % 500 > 0)
            {
                TotalPages = Convert.ToInt32(totalCount / 500 + 1);
            }
            else
            {
                TotalPages = Convert.ToInt32(totalCount / 500);
            }

            Logs = await Task.Run(() =>
            {
                IEnumerable<Log> _logs = mongoDatabase.GetCollection<BsonDocument>("log").Find(filter)
                 .Sort(sort)
                 .Skip(500 * (Page - 1))
                 .Limit(500)
                 .ToList()
                 .Select(x =>
                 {
                     Log log = new Log()
                     {
                         User = (string)x["user"],
                         Module = (string)x["module"],
                         Menu = (string)x["menu"],
                         Logtime = (new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc).AddMilliseconds(Convert.ToInt64((long)x["logtime"]))).ToString("yyyy-MM-dd HH:mm:ss"),
                         Description = (string)x["description"],
                         Action = (string)x["action"]
                     };
                     return log;
                 });
                return new ObservableCollection<Log>(_logs);
            });
            _viewModel.NotifyChange("Operations");
        }
        private void Pre_Page(object sender, RoutedEventArgs e)
        {
            if (Page > 1)
            {
                Page = Page - 1;
            }
            LoadData();
        }

        public int Page { get; set; }
        public int TotalPages { get; set; }
        public string SearchText { get; set; }
        
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

        private void Search_Button(object sender, RoutedEventArgs e)
        {
            LoadData();
        }

        private void Download_Click(object sender, RoutedEventArgs e)
        {

        }

        public ObservableCollection<Log> Logs { get; set; }

        private void Control_Unloaded(object sender, RoutedEventArgs e)
        {
            _viewModel.ProcessAtControl = null;
            _viewModel.Operations = null;
        }
    }
}

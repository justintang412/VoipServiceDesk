using Arco.Services;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Windows;
using System.Windows.Controls;

namespace Arco.Views
{
    /// <summary>
    /// SystemConfig.xaml 的交互逻辑
    /// </summary>
    public partial class SystemConfig : UserControl
    {
        private MainWindowViewModel _viewModel = null;
        public SystemConfig()
        {
            InitializeComponent();
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
        private void Control_Loaded(object sender, RoutedEventArgs e)
        {
            _viewModel = (DataContext as MainWindowViewModel);
            _viewModel.SystemConfig = this;
            BsonDocument info = DataService.GetInstance().Database.GetCollection<BsonDocument>("systemconfig")
                .Find(Builders<BsonDocument>.Filter.Empty)
                .First();
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
            _viewModel.NotifyChange("SystemConfig");
        }

        private void Control_Unloaded(object sender, RoutedEventArgs e)
        {
            _viewModel.ProcessAtControl = null;
            _viewModel.SystemConfig = null;
        }

        private void Save(object sender, RoutedEventArgs e)
        {
            if (Pbxmode == null ||
                ExtFrom == null ||
                ExtTo == null ||
                MeetingFrom == null ||
                MeetingTo == null ||
                BroadcastingFrom == null ||
                BroadcastingTo == null ||
                Calltimeout == null ||
                Rollcalltimeout == null ||
                Contactlimit == null ||
                License == null
                ) return;

            UpdateDefinition<BsonDocument> update = Builders<BsonDocument>.Update
                .Set("pbxmode", Pbxmode)
                .Set("extFrom", ExtFrom)
                .Set("extTo", ExtTo)
                .Set("meetingFrom", MeetingFrom)
                .Set("meetingTo", MeetingTo)
                .Set("broadcastingFrom", BroadcastingFrom)
                .Set("broadcastingTo", BroadcastingTo)
                .Set("calltimeout", Calltimeout)
                .Set("rollcalltimeout", Rollcalltimeout)
                .Set("contactlimit", Contactlimit)
                .Set("license", License);

            DataService.GetInstance().Database.GetCollection<BsonDocument>("systemconfig").UpdateMany(Builders<BsonDocument>.Filter.Empty, update);
        }
    }
}

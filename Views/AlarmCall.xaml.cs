using Arco.Models;
using Arco.Services;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Arco.Views
{
    /// <summary>
    /// AlarmCall.xaml 的交互逻辑
    /// </summary>
    public partial class AlarmCall : UserControl
    {
        private MainWindowViewModel _viewModel = null;
        public AlarmCall()
        {
            InitializeComponent();
        }
        private void Control_Loaded(object sender, RoutedEventArgs e)
        {
            _viewModel = (DataContext as MainWindowViewModel);
            _viewModel.AlarmCall = this;
            _viewModel.NotifyChange("AlarmCall");
        }
        private void Control_Unloaded(object sender, RoutedEventArgs e)
        {
            _viewModel.ProcessAtControl = null;
            _viewModel.AlarmCall = null;
        }

        public AlarmInfo NewAlarmInfo { get; set; }
        public AlarmInfo ChosenAlarmInfo { get; set; }
        public ObservableCollection<AlarmInfo> AlarmInfos { get; set; }

        private CustomDialog CustomDialogAdd { get; set; }

        private async void CustomDialogAddClose(object sender, RoutedEventArgs e)
        {
            await ((MainWindow)Application.Current.MainWindow).HideMetroDialogAsync(CustomDialogAdd);
        }
        private async void Add_Click(object sender, RoutedEventArgs e)
        {
            NewAlarmInfo = new AlarmInfo();


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
                topGrid.FindChild<Label>().SetCurrentValue(ContentProperty, "新增报警联动呼叫");
                (topGrid.Children[2] as Button).Click += CustomDialogAddClose;
            }
            else
            {
                Grid topGrid = CustomDialogAdd.DialogTop as Grid;
                topGrid.FindChild<Label>().SetCurrentValue(ContentProperty, "新增报警联动呼叫");
            }
            await mainWindow.ShowMetroDialogAsync(CustomDialogAdd);
        }

        private async void Edit_Click(object sender, RoutedEventArgs e)
        {
            if (ChosenAlarmInfo == null)
            {
                _viewModel.Message = "请选择一个报警联动定义";
                return;
            }
            NewAlarmInfo = ChosenAlarmInfo;

            _viewModel.NotifyChange("AlarmCall");
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
            if (AlarmInfo_DataGrid.SelectedItem == null)
            {
                _viewModel.Message = "请选择一个报警联动定义";
                return;
            }
            ChosenAlarmInfo = AlarmInfo_DataGrid.SelectedItem as AlarmInfo;
            _viewModel.Message = "正在删除报警联动定义...";
            DataService.GetInstance().Database
                .GetCollection<BsonDocument>("alarminfo")
                .DeleteOne(Builders<BsonDocument>.Filter.Eq("_id", new ObjectId(ChosenAlarmInfo.Id)));
            _viewModel.AlarmInfos = DataService.GetInstance().Database.GetCollection<BsonDocument>("alarminfo")
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
            _viewModel.Message += "成功";
            _viewModel.NotifyChange("AlarmInfos");
        }

        private async void Save_AlarmInfo(object sender, RoutedEventArgs e)
        {
            if (NewAlarmInfo == null
                ||NewAlarmInfo.Ivr==null||
                NewAlarmInfo.Name==null||
                NewAlarmInfo.Remark==null||
                NewAlarmInfo.Token==null||
                NewAlarmInfo.CalleeNumber==null||
                NewAlarmInfo.Code==null
                ) return;
            _viewModel.Message = "正在保存报警联动定义...";

            if (NewAlarmInfo != null && NewAlarmInfo.Id != null)
            {
                DataService.GetInstance().Database
                    .GetCollection<BsonDocument>("alarminfo")
                    .DeleteOne(Builders<BsonDocument>.Filter.Eq("_id", new ObjectId(NewAlarmInfo.Id)));
            }
            DataService.GetInstance().Database
                    .GetCollection<BsonDocument>("alarminfo")
                    .InsertOne(new BsonDocument {
                        {"code",NewAlarmInfo.Code},
                        {"name",NewAlarmInfo.Name },
                        {"ivr", NewAlarmInfo.Ivr},
                        {"token",NewAlarmInfo.Token },
                        {"calleenumber",NewAlarmInfo.CalleeNumber },
                        {"remark",NewAlarmInfo.Remark }
                    });
            _viewModel.AlarmInfos = DataService.GetInstance().Database.GetCollection<BsonDocument>("alarminfo")
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
            _viewModel.Message += "成功";
            _viewModel.NotifyChange("AlarmInfos");
            await ((MainWindow)Application.Current.MainWindow).HideMetroDialogAsync(CustomDialogAdd);
        }

        private void AlarmInfos_DataGrid_Seletion_Changed(object sender, SelectionChangedEventArgs e)
        {
            ChosenAlarmInfo = (sender as DataGrid).SelectedItem as AlarmInfo;
        }

        
    }
}

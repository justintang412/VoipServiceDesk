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
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Arco.Views
{
    /// <summary>
    /// Users.xaml 的交互逻辑
    /// </summary>
    public partial class Users : UserControl
    {
        private MainWindowViewModel _viewModel = null;
        public Users()
        {
            InitializeComponent();
        }
        public User NewUser { get; set; }
        public User ChosenUser { get; set; }

        public ObservableCollection<User> MyUsers { get; set; }
        private CustomDialog CustomDialogAdd { get; set; }

        private async void CustomDialogAddClose(object sender, RoutedEventArgs e)
        {
            await ((MainWindow)Application.Current.MainWindow).HideMetroDialogAsync(CustomDialogAdd);
        }
        private async void Add_Click(object sender, RoutedEventArgs e)
        {
            NewUser = new User();
            NewUser.UserConfig = new UserConfig();

            MainWindow mainWindow = Application.Current.MainWindow as MainWindow;

            if (CustomDialogAdd == null)
            {
                CustomDialogAdd = new CustomDialog(mainWindow.MetroDialogOptions)
                {
                    Content = this.Resources["CustomDialogAdd"],
                    DialogContentMargin = new GridLength(0),
                    Width = 500,
                    BorderThickness = new Thickness(0),
                    BorderBrush = Brushes.DarkGray,
                    Background = new SolidColorBrush(Color.FromRgb(16, 57, 77 )),//#10394d
                    DialogTop = this.Resources["CustomDialogAddClose"]
                };
                Grid topGrid = CustomDialogAdd.DialogTop as Grid;
                topGrid.FindChild<Label>().SetCurrentValue(ContentProperty, "新增");
                (topGrid.Children[2] as Button).Click += CustomDialogAddClose;
            }

            await mainWindow.ShowMetroDialogAsync(CustomDialogAdd);
            this.CustomDialogAdd.FindChild<TextBox>("NewUser_Name").SetCurrentValue(IsEnabledProperty, true);
        }

        private async void Edit_Click(object sender, RoutedEventArgs e)
        {
            if (ChosenUser == null)
            {
                _viewModel.Message = "请选择一个用户";
                return;
            }
            NewUser = ChosenUser;

            _viewModel.NotifyChange("ViewUsers");
            MainWindow mainWindow = Application.Current.MainWindow as MainWindow;

            if (CustomDialogAdd == null)
            {
                CustomDialogAdd = new CustomDialog(mainWindow.MetroDialogOptions)
                {
                    Content = this.Resources["CustomDialogAdd"],
                    DialogContentMargin = new GridLength(5),
                    Width = 500,
                    Height = 260,
                    BorderThickness = new Thickness(2),
                    BorderBrush = Brushes.DarkGray,
                    DialogTop = this.Resources["CustomDialogAddClose"]
                };
                Grid topGrid = CustomDialogAdd.DialogTop as Grid;
                topGrid.FindChild<Label>().SetCurrentValue(ContentProperty, "修改");
                (topGrid.Children[2] as Button).Click += CustomDialogAddClose;
            }

            await mainWindow.ShowMetroDialogAsync(CustomDialogAdd);
            this.CustomDialogAdd.FindChild<TextBox>("NewUser_Name").SetCurrentValue(IsEnabledProperty, false);

        }

        private void Del_Click(object sender, RoutedEventArgs e)
        {
            if (ChosenUser == null)
            {
                _viewModel.Message = "请选择一个用户";
                return;
            }
            FilterDefinition<BsonDocument> filter = Builders<BsonDocument>.Filter.Eq("user", ChosenUser.Username);
            IMongoCollection<BsonDocument> mongoColleciton = DataService.GetInstance().Database.GetCollection<BsonDocument>("sysuser");
            mongoColleciton.DeleteOne(filter);
            ChosenUser = null;

            LoadUser();
        }

        private void Control_Loaded(object sender, RoutedEventArgs e)
        {
            _viewModel = (DataContext as MainWindowViewModel);
            _viewModel.ViewUsers = this;
            LoadUser();
        }

        private void Control_Unloaded(object sender, RoutedEventArgs e)
        {
            _viewModel.ProcessAtControl = null;
            _viewModel.ViewUsers = null;
        }

        private async void User_Datagrid_Seletion_Changed(object sender, SelectionChangedEventArgs e)
        {
            object obj = (sender as DataGrid)?.SelectedItem;
            if (obj == null) return;
            ChosenUser = obj as User;
            _viewModel.NotifyChange("ViewUsers");
        }

        private async void Save(object sender, RoutedEventArgs e)
        {
            if (NewUser == null||NewUser.Password==null || NewUser.UserConfig==null || NewUser.Username==null||
                NewUser.UserConfig.Extno1==null || 
                !Core.Utils.IsNumberInRange(NewUser.UserConfig.Extno1, Models.SystemConfig.Instance.ExtFrom, Models.SystemConfig.Instance.ExtTo)||
                NewUser.UserConfig.Extno2 == null ||
                !Core.Utils.IsNumberInRange(NewUser.UserConfig.Extno2, Models.SystemConfig.Instance.ExtFrom, Models.SystemConfig.Instance.ExtTo) ||
                NewUser.UserConfig.Localip==null||
                NewUser.UserConfig.Localport==null||
                NewUser.UserConfig.Pbxpass==null||
                NewUser.UserConfig.Pbxuser==null||
                NewUser.UserConfig.Queue==null||
                NewUser.UserConfig.QueuePassword==null||
                NewUser.UserConfig.Serverip==null||
                NewUser.UserConfig.Serverport==null
                ) return;
            _viewModel.Message = "正在保存用户信息...";

            long count = DataService.GetInstance().Database.GetCollection<BsonDocument>("sysuser")
                .Find(new BsonDocument { { "user", NewUser.Username } }).CountDocuments();

            if (count > 0)
            {
                FilterDefinition<BsonDocument> filter = Builders<BsonDocument>.Filter.Eq("user", NewUser.Username);
                UpdateDefinition<BsonDocument> update = Builders<BsonDocument>.Update
                    .Set("pass", CustomDialogAdd.FindChild<PasswordBox>("MyPasswordBox").Password.ToMD5())
                    .Set("isadmin", NewUser.Isadmin ? "true" : "false")
                    .Set("config.localip", NewUser.UserConfig.Localip)
                    .Set("config.localport", NewUser.UserConfig.Localport)
                    .Set("config.serverip", NewUser.UserConfig.Serverip)
                    .Set("config.serverport", NewUser.UserConfig.Serverport)
                    .Set("config.pbxuser", NewUser.UserConfig.Pbxuser)
                    .Set("config.pbxpass", CustomDialogAdd.FindChild<PasswordBox>("PBXPasswordBox").Password.ToMD5())
                    .Set("config.extno1", NewUser.UserConfig.Extno1)
                    .Set("config.extno2", NewUser.UserConfig.Extno2)
                    .Set("config.queue", NewUser.UserConfig.Queue)
                    .Set("config.queuepass", CustomDialogAdd.FindChild<PasswordBox>("PBXQueuePasswordBox").Password.ToMD5());
                DataService.GetInstance().Database.GetCollection<BsonDocument>("sysuser").UpdateOne(filter, update);
            }
            else
            {
                BsonDocument bsonDocument = new BsonDocument {
                    {"user",NewUser.Username },
                    { "pass", CustomDialogAdd.FindChild<PasswordBox>("MyPasswordBox").Password.ToMD5() },
                    { "isadmin", NewUser.Isadmin?"true":"false"},
                    { "config.localip", NewUser.UserConfig.Localip},
                    { "config.localport", NewUser.UserConfig.Localport},
                    { "config.serverip", NewUser.UserConfig.Serverip},
                    { "config.serverport", NewUser.UserConfig.Serverport},
                    { "config.pbxuser", NewUser.UserConfig.Pbxuser},
                    { "config.pbxpass", CustomDialogAdd.FindChild<PasswordBox>("PBXPasswordBox").Password.ToMD5()},
                    { "config.extno1", NewUser.UserConfig.Extno1},
                    { "config.extno2", NewUser.UserConfig.Extno2},
                    { "config.queue", NewUser.UserConfig.Queue},
                    { "config.queuepass",CustomDialogAdd.FindChild<PasswordBox>("PBXQueuePasswordBox").Password.ToMD5()}
                };
                DataService.GetInstance().Database.GetCollection<BsonDocument>("sysuser").InsertOne(bsonDocument);
            }
            await ((MainWindow)Application.Current.MainWindow).HideMetroDialogAsync(CustomDialogAdd);
            LoadUser();
            _viewModel.Message += "成功";
        }


        private void LoadUser()
        {
            FilterDefinition<BsonDocument> filter = null;
            if (!_viewModel.User.Isadmin)
            {
                filter = Builders<BsonDocument>.Filter.Eq("user", _viewModel.User.Username);
            }
            else
            {
                filter = Builders<BsonDocument>.Filter.Empty;
            }

            MyUsers = new ObservableCollection<User>(
                    DataService.GetInstance().Database.GetCollection<BsonDocument>("sysuser")
                        .Find(filter)
                        .ToList()
                        .Select(userinfoDocument =>
                        {
                            User user = new User
                            {
                                Username = (string)userinfoDocument["user"],
                                Password = (string)userinfoDocument["pass"],
                                Isadmin = ((string)userinfoDocument["isadmin"]).Equals("true"),
                                UserConfig = new UserConfig()
                                {
                                    Localip = (string)userinfoDocument["config"]["localip"],
                                    Localport = (string)userinfoDocument["config"]["localport"],
                                    Serverip = (string)userinfoDocument["config"]["serverip"],
                                    Serverport = (string)userinfoDocument["config"]["serverport"],
                                    Pbxuser = (string)userinfoDocument["config"]["pbxuser"],
                                    Pbxpass = (string)userinfoDocument["config"]["pbxpass"],
                                    Extno1 = (string)userinfoDocument["config"]["extno1"],
                                    Extno2 = (string)userinfoDocument["config"]["extno2"],
                                    Queue = (string)userinfoDocument["config"]["queue"],
                                    QueuePassword = (string)userinfoDocument["config"]["queuepass"]
                                }
                            };
                            return user;

                        })
                    );
            _viewModel.NotifyChange("ViewUsers");
        }
        
    }
}

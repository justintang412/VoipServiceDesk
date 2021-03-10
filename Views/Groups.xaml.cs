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
    /// Groups.xaml 的交互逻辑
    /// </summary>
    public partial class Groups : UserControl
    {
        private MainWindowViewModel _viewModel = null;
        public Groups()
        {
            InitializeComponent();
        }
        public Group NewGroup { get; set; }
        public Group ChosenGroup { get; set; }
        public Contact ChosenContact { get; set; }
        public List<string> GroupTypes { get; set; }
        public string ContactSearchText { get; set; }

        public CallHistory ChosenCallHistory { get; set; }
        public bool IsOnlyFavorite { get; set; }
        public string Music { get; set; }
        public int PlayCount { get; set; }
        public ObservableCollection<Contact> Contacts { get; set; }
        private void Control_Loaded(object sender, RoutedEventArgs e)
        {
            _viewModel = (DataContext as MainWindowViewModel);
            _viewModel.ViewGroups = this;
            _viewModel.ProcessAtControl = this.ProcessAtControl;
            GroupTypes = new List<string>();
            GroupTypes.Add("单向传呼");
            GroupTypes.Add("双向对讲");
            GroupTypes.Add("单向组播");
        }

        private void ProcessAtControl(params object[] values)
        {
            JObject result = values[0] as JObject;
            string action = (string)result["action"];
            if (action.Equals("ExtensionStatus"))
            {
                ChosenGroupContacts_grid.Items.Refresh();
                return;
            }


            if (action.Equals("ANSWER") || action.Equals("ANSWERED"))
            {
                ChosenGroupContacts_grid.Items.Refresh();
                return;
            }
            if (action.Equals("CallFailed"))
            {
                ChosenGroupContacts_grid.Items.Refresh();
                return;
            }
            if (action.Equals("BYE"))
            {
                ChosenGroupContacts_grid.Items.Refresh();
                return;
            }
        }

        private void Control_Unloaded(object sender, RoutedEventArgs e)
        {
            _viewModel.ProcessAtControl = null;
            _viewModel.ViewGroups = null;
        }

        private CustomDialog CustomDialogAdd { get; set; }

        private async void CustomDialogAddClose(object sender, RoutedEventArgs e)
        {
            await ((MainWindow)Application.Current.MainWindow).HideMetroDialogAsync(CustomDialogAdd);
        }
        private async void Add_Click(object sender, RoutedEventArgs e)
        {
            NewGroup = new Group();
            NewGroup.AllowExten = new ObservableCollection<Contact>();

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
                topGrid.FindChild<Label>().SetCurrentValue(ContentProperty, "新增分组");
                (topGrid.Children[2] as Button).Click += CustomDialogAddClose;
            }
            else
            {
                Grid topGrid = CustomDialogAdd.DialogTop as Grid;
                topGrid.FindChild<Label>().SetCurrentValue(ContentProperty, "新增分组");
            }

            await mainWindow.ShowMetroDialogAsync(CustomDialogAdd);
            this.CustomDialogAdd.FindChild<TextBox>("NewGroup_Number").SetCurrentValue(IsEnabledProperty, true);
        }

        private async void Edit_Click(object sender, RoutedEventArgs e)
        {
            if (ChosenGroup == null)
            {
                _viewModel.Message = "请选择一个组";
                return;
            }
            NewGroup = ChosenGroup;

            _viewModel.NotifyChange("ViewGroup");
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
                topGrid.FindChild<Label>().SetCurrentValue(ContentProperty, "修改分组");
                (topGrid.Children[2] as Button).Click += CustomDialogAddClose;
            }
            else
            {
                Grid topGrid = CustomDialogAdd.DialogTop as Grid;
                topGrid.FindChild<Label>().SetCurrentValue(ContentProperty, "修改分组");
            }

            await mainWindow.ShowMetroDialogAsync(CustomDialogAdd);
            this.CustomDialogAdd.FindChild<TextBox>("NewGroup_Number").SetCurrentValue(IsEnabledProperty, false);
        }

        private async void Del_Click(object sender, RoutedEventArgs e)
        {
            if (Groups_DataGrid.SelectedItem == null)
            {
                _viewModel.Message = "请选择一个组";
                return;
            }
            ChosenGroup = Groups_DataGrid.SelectedItem as Group;

            JObject response = await DataService.GetInstance().PostAsync("{\"number\": \"" + ChosenGroup.Number + "\"}", "/paginggroup/delete", true);
            if (response != null && ((string)response["status"]).Equals("Success"))
            {
                FilterDefinition<BsonDocument> filter = Builders<BsonDocument>.Filter.Eq("number", ChosenGroup.Number);
                IMongoCollection<BsonDocument> mongoColleciton = DataService.GetInstance().Database.GetCollection<BsonDocument>("groups");
                mongoColleciton.DeleteOne(filter);
                _viewModel.Message = ChosenGroup.Name + "信息删除成功。";
                _viewModel.Groups.Remove(ChosenGroup);
                ChosenGroup = null;
            }

            _viewModel.NotifyChange("Groups");
        }

        private async void save()
        {
            if (NewGroup == null || NewGroup.Name == null || NewGroup.Number == null ||
                NewGroup.AllowExten == null || NewGroup.AllowExten.Count == 0) {
                _viewModel.Message = "字段、组成员等信息不能为空";
                return;
            }
            if (!Core.Utils.IsNumberInRange(NewGroup.Number, Models.SystemConfig.Instance.BroadcastingFrom, Models.SystemConfig.Instance.BroadcastingTo) ||
                NewGroup.AllowExten == null)
            {
                _viewModel.Message = "广播组号码超出广播组当前设置范围："+ Models.SystemConfig.Instance.BroadcastingFrom + " - "+ Models.SystemConfig.Instance.BroadcastingTo;
                return;
            }
            _viewModel.Message = "正在保存分组信息...";
            bool isUpdate = false;
            if (NewGroup != null && NewGroup.Groupid != null)
            {
                isUpdate = true;
            }
            string memberstring = "";
            foreach (Contact c in NewGroup.AllowExten)
            {
                memberstring += c.Devices[0].Number + ",";
            }
            if (memberstring.Length > 0)
            {
                memberstring = memberstring.Substring(0, memberstring.Length - 1);
            }

            string url = isUpdate ? "/paginggroup/update" : "/paginggroup/add";
            string json = isUpdate ? "{\"id\":\""
                    + NewGroup.Groupid + "\",\"number\": \""
                     + NewGroup.Number + "\",\"name\": \""
                     + NewGroup.Name + "\",\"duplex\": \""
                     + NewGroup.Duplex.ToGroupType() + "\",\"allowexten\": \""
                     + memberstring + "\",\"allowextengroup\": \"\",\"enablekeyhanup\": \"yes\", \""
                     + NewGroup.MulticastIp + "\":\"\"}"
                     : "{\"number\": \""
                     + NewGroup.Number + "\",\"name\": \""
                     + NewGroup.Name + "\",\"duplex\": \""
                     + NewGroup.Duplex.ToGroupType() + "\",\"allowexten\": \""
                     + memberstring + "\",\"allowextengroup\": \"\",\"enablekeyhanup\": \"yes\", \""
                     + NewGroup.MulticastIp + "\":\"\"}";

            JObject responseUpdate = await DataService.GetInstance().PostAsync(json, url, true);
            if (responseUpdate != null && ((string)responseUpdate["status"]).Equals("Success"))
            {

                JObject queryGroupInfoResponse = await DataService.GetInstance().PostAsync("{\"number\":\"" + NewGroup.Number + "\"}", "/paginggroup/query", true);
                if (queryGroupInfoResponse != null && ((string)queryGroupInfoResponse["status"]).Equals("Success"))
                {
                    NewGroup.Groupid = (string)((JArray)queryGroupInfoResponse["paginggroup"])[0]["id"];
                }
                if (NewGroup.Groupid == null)
                {
                    _viewModel.Message += "失败";
                    await ((MainWindow)Application.Current.MainWindow).HideMetroDialogAsync(CustomDialogAdd);
                    return;
                }


                DataService.GetInstance().Database
                    .GetCollection<BsonDocument>("groups")
                    .DeleteOne(Builders<BsonDocument>.Filter.Eq("number", NewGroup.Number));
                DataService.GetInstance().Database
                    .GetCollection<BsonDocument>("groups")
                    .InsertOne(new BsonDocument {
                        {"groupid", NewGroup.Groupid??""},
                        {"number",NewGroup.Number??"" },
                        {"name", NewGroup.Name??""},
                        {"duplex",NewGroup.Duplex.ToGroupType() },
                        {"allowexten",memberstring??"" },
                        {"allowextengroup","" },
                        {"enablekeyhanup","yes" }
                    });
                _viewModel.Message += "成功";
                _viewModel.Groups = new ObservableCollection<Group>(DataService.GetInstance().QueryGroups(_viewModel.BaseContacts));
                _viewModel.NotifyChange("Groups");
                
                return;
            }
        }

        private async void Save_Group(object sender, RoutedEventArgs e)
        {
            save();
            await ((MainWindow)Application.Current.MainWindow).HideMetroDialogAsync(CustomDialogAdd);
            
        }

        private void Remove_Member_Click(object sender, RoutedEventArgs e)
        {
            if (ChosenGroupContacts_grid.SelectedItem == null) return;
            ChosenGroup.AllowExten.Remove(ChosenGroupContacts_grid.SelectedItem as Contact);

            NewGroup = ChosenGroup;
            save();
        }

        private void Add_Member_Click(object sender, RoutedEventArgs e)
        {
            if (Contact_Grid.SelectedItem == null) return;
            if (ChosenGroup == null) return;
            if (ChosenGroup.AllowExten == null)
            {
                ChosenGroup.AllowExten = new ObservableCollection<Contact>();
            }
            ChosenGroup.AllowExten.Add(Contact_Grid.SelectedItem as Contact);
            NewGroup = ChosenGroup;
            save();
        }

        private void ChosenGroupContacts_grid_Changed(object sender, SelectionChangedEventArgs e)
        {
            ChosenContact = (sender as DataGrid)?.SelectedItem as Contact;
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
            _viewModel.NotifyChange("ViewGroups");
        }


       

        private void Groups_DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Groups_DataGrid.SelectedItem != null)
            {
                ChosenGroup = Groups_DataGrid.SelectedItem as Group;
            }
            _viewModel.NotifyChange("ViewGroups");
        }

        private void Source_Contact_Grid_Double_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender != null && sender is DataGrid)
            {
                DataGrid dataGrid = sender as DataGrid;
                if (dataGrid.SelectedItem != null && dataGrid.SelectedItem is Contact && NewGroup != null)
                {
                    if (NewGroup.AllowExten == null)
                    {
                        NewGroup.AllowExten = new ObservableCollection<Contact>();

                    }
                    if (!NewGroup.AllowExten.Contains(dataGrid.SelectedItem as Contact))
                    {
                        NewGroup.AllowExten.Add(dataGrid.SelectedItem as Contact);
                        _viewModel.NotifyChange("ViewGroups");
                    }
                }
            }
        }

        private void Dest_Contact_Grid_Double_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender != null && sender is DataGrid)
            {
                DataGrid dataGrid = sender as DataGrid;
                if (dataGrid.SelectedItem != null && dataGrid.SelectedItem is Contact && NewGroup != null)
                {
                    if (NewGroup.AllowExten == null)
                    {
                        NewGroup.AllowExten = new ObservableCollection<Contact>();

                    }
                    if (NewGroup.AllowExten.Contains(dataGrid.SelectedItem as Contact))
                    {
                        NewGroup.AllowExten.Remove(dataGrid.SelectedItem as Contact);
                        _viewModel.NotifyChange("ViewGroups");
                    }
                }
            }
        }

        private async void GroupCall_Click(object sender, RoutedEventArgs e)
        {
            if (Groups_DataGrid.SelectedItem == null) return;
            Models.Group group = (Groups_DataGrid.SelectedItem as Models.Group);
            if (group == null) return;

            _viewModel.Message = "正在呼叫" + group.Number + "...";
            JObject response = await DataService.GetInstance().PostAsync("{" +
                                    "\"caller\": \"" + _viewModel.User.UserConfig.Extno1 + "\"," +
                                    "\"callee\": \"" + group.Number + "\"}", "/extension/dial_number", true);

            if (response != null && ((string)response["status"]).Equals("Success"))
            {
                _viewModel.Message += "成功";
                _viewModel.NotifyChange("Message");
            }
        }

        private CustomDialog CustomDialogPlay { get; set; }

        private async void CustomDialogPlayClose(object sender, RoutedEventArgs e)
        {
            await ((MainWindow)Application.Current.MainWindow).HideMetroDialogAsync(CustomDialogPlay);
        }
        
        private async void GroupPlay_Click(object sender, RoutedEventArgs e)
        {
            if (Groups_DataGrid.SelectedItem == null) return;
            Models.Group group = (Groups_DataGrid.SelectedItem as Models.Group);
            if (group == null) return;
            PlayCount = 1;
            MainWindow mainWindow = Application.Current.MainWindow as MainWindow;

            if (CustomDialogPlay == null)
            {
                CustomDialogPlay = new CustomDialog(mainWindow.MetroDialogOptions)
                {
                    Content = this.Resources["CustomDialogPlay"],
                    DialogContentMargin = new GridLength(5),
                    Width = 400,
                    Height = 200,
                    BorderThickness = new Thickness(2),
                    BorderBrush = Brushes.DarkGray,
                    DialogTop = this.Resources["CustomDialogPlayClose"]
                };
                Grid topGrid = CustomDialogPlay.DialogTop as Grid;
                topGrid.FindChild<Label>().SetCurrentValue(ContentProperty, "语音广播");
                (topGrid.Children[2] as Button).Click += CustomDialogPlayClose;
            }

            await mainWindow.ShowMetroDialogAsync(CustomDialogPlay);
        }

        private async void Dialog_GroupPlay_Click(object sender, RoutedEventArgs e)
        {
            if (Music == null || Music.Length == 0) return;
            if (PlayCount <0) return;
            if (Groups_DataGrid.SelectedItem == null) return;
            (sender as Button).SetCurrentValue(IsEnabledProperty, false);
            _viewModel.Message = "正在播放" + Music + "...";
            JObject response = await DataService.GetInstance().PostAsync("{\"count\":\""+ PlayCount + "\",\"prompt\":\""+ Music + "\"," +
                                    "\"volume\": \"\"," +
                                    "\"callee\": \"" + (Groups_DataGrid.SelectedItem as Group).Number + "\"}", "/extension/dial_number", true);

            if (response != null && ((string)response["status"]).Equals("Success"))
            {
                _viewModel.Message += "成功";
                _viewModel.NotifyChange("Message");
                await ((MainWindow)Application.Current.MainWindow).HideMetroDialogAsync(CustomDialogPlay);
            }
             (sender as Button).SetCurrentValue(IsEnabledProperty, true);
        }
    }
}

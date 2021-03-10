using Arco.Models;
using Arco.Services;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Arco.Views
{
    /// <summary>
    /// UserMeetings.xaml 的交互逻辑
    /// </summary>
    public partial class UserMeetings : UserControl
    {
        private MainWindowViewModel _viewModel = null;
        public UserMeetings()
        {
            InitializeComponent();
        }
        public Meeting NewMeeting { get; set; }
        public Meeting ChosenMeeting { get; set; }
        public ContactWithSingleDevice ChosenContact { get; set; }
        public List<string> MeetingTypes { get; set; }
        public string ContactSearchText { get; set; }

        public bool IsOnlyFavorite { get; set; }
        public ObservableCollection<Contact> Contacts { get; set; }
        private void Control_Loaded(object sender, RoutedEventArgs e)
        {
            _viewModel = (DataContext as MainWindowViewModel);
            _viewModel.UserMeetings = this;
            _viewModel.ProcessAtControl = this.ProcessAtControl;
        }

        private void ProcessAtControl(params object[] values)
        {
            ChosenMeetingContacts_grid.Items.Refresh();
            if (CustomDialogMeeting != null)
            {
                this.CustomDialogMeeting.FindChild<DataGrid>("MeetingMembersGrid")?.Items.Refresh();
            }
        }

        private void Control_Unloaded(object sender, RoutedEventArgs e)
        {
            _viewModel.ProcessAtControl = null;
            _viewModel.UserMeetings = null;
        }

        private CustomDialog CustomDialogAdd { get; set; }

        private async void CustomDialogAddClose(object sender, RoutedEventArgs e)
        {
            await ((MainWindow)Application.Current.MainWindow).HideMetroDialogAsync(CustomDialogAdd);
        }
        private async void Add_Click(object sender, RoutedEventArgs e)
        {
            NewMeeting = new Meeting();
            NewMeeting.Members = new ObservableCollection<ContactWithSingleDevice>();

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
                topGrid.FindChild<Label>().SetCurrentValue(ContentProperty, "新增会议");
                (topGrid.Children[2] as Button).Click += CustomDialogAddClose;
            }
            else
            {
                Grid topGrid = CustomDialogAdd.DialogTop as Grid;
                topGrid.FindChild<Label>().SetCurrentValue(ContentProperty, "新增会议");
            }

            await mainWindow.ShowMetroDialogAsync(CustomDialogAdd);
        }

        private async void Edit_Click(object sender, RoutedEventArgs e)
        {
            if (ChosenMeeting == null)
            {
                _viewModel.Message = "请选择一个会议";
                return;
            }
            NewMeeting = ChosenMeeting;

            _viewModel.NotifyChange("UserMeetings");
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
            if (Meetings_DataGrid.SelectedItem == null)
            {
                _viewModel.Message = "请选择一个会议";
                return;
            }
            ChosenMeeting = Meetings_DataGrid.SelectedItem as Meeting;

            FilterDefinition<BsonDocument> filter = Builders<BsonDocument>.Filter.Eq("name", ChosenMeeting.Name);
            IMongoCollection<BsonDocument> mongoColleciton = DataService.GetInstance().Database.GetCollection<BsonDocument>("meetings");
            mongoColleciton.DeleteOne(filter);
            _viewModel.Message = ChosenMeeting.Name + "信息删除成功。";
            LoadMeeting();
            ChosenMeeting = null;
        }

        private async void save()
        {
            if (NewMeeting == null ||
                NewMeeting.Content == null ||
                NewMeeting.Members == null ||
                NewMeeting.Members.Count == 0 ||
                NewMeeting.Name == null ||
                NewMeeting.Number == null ||
                !Core.Utils.IsNumberInRange(NewMeeting.Number, Models.SystemConfig.Instance.MeetingFrom, Models.SystemConfig.Instance.MeetingTo) ||
                NewMeeting.Starttime == null
                ) {
                _viewModel.Message = "会议内容、成员、会议名称，不能为空，会议室必须事先在PBX中创建，号码必须在" +
                    Models.SystemConfig.Instance.MeetingFrom + " - " +
                    Models.SystemConfig.Instance.MeetingTo + "之间";
                return;
            }
            _viewModel.Message = "正在保存会议信息...";
            DataService.GetInstance().Database
                    .GetCollection<BsonDocument>("meetings")
                    .DeleteOne(Builders<BsonDocument>.Filter.Eq("name", NewMeeting.Name));
            BsonArray bsonArray = new BsonArray();
            foreach (ContactWithSingleDevice c in NewMeeting.Members)
            {
                bsonArray.Add(c.Device.Number);
            }
            DataService.GetInstance().Database
                .GetCollection<BsonDocument>("meetings")
                .InsertOne(new BsonDocument {
                        {"number",NewMeeting.Number },
                        {"name", NewMeeting.Name??""},
                        {"content",NewMeeting.Content??""},
                        {"starttime",NewMeeting.Starttime??""},
                        {"members",bsonArray }
                });
            _viewModel.Message += "成功";
            _viewModel.Meetings = new ObservableCollection<Meeting>(DataService.GetInstance().QueryMeetings(_viewModel));
            _viewModel.NotifyChange("Meetings");
        }


        private void ChosenMeetingContacts_grid_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (ChosenMeetingContacts_grid.SelectedItem != null)
            {
                ChosenContact = ChosenMeetingContacts_grid.SelectedItem as ContactWithSingleDevice;
                _viewModel.DialNo = ChosenContact.Device.Number;
                _viewModel.NotifyChange("UserMeetings");
            }

        }


        private void LoadMeeting()
        {
            _viewModel.Meetings = new ObservableCollection<Meeting>(DataService.GetInstance().QueryMeetings(_viewModel));
            _viewModel.NotifyChange("Meetings");
        }

        private void Source_Contact_Grid_Double_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender != null && sender is DataGrid)
            {
                DataGrid dataGrid = sender as DataGrid;
                if (dataGrid.SelectedItem != null && dataGrid.SelectedItem is Contact && NewMeeting != null)
                {
                    if (NewMeeting.Members == null)
                    {
                        NewMeeting.Members = new ObservableCollection<ContactWithSingleDevice>();

                    }
                    ContactWithSingleDevice cwsd = NewMeeting.Members.ToList().Find(x => x.Contact.Name.Equals((dataGrid.SelectedItem as Contact).Name));
                    if (cwsd == null)
                    {
                        Contact c = dataGrid.SelectedItem as Contact;
                        NewMeeting.Members.Add(new ContactWithSingleDevice
                        {
                            Contact = c,
                            Device = c.Devices[0]
                        });
                        _viewModel.NotifyChange("UserMeetings");
                    }
                }
            }
        }

        private void Dest_Contact_Grid_Double_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender != null && sender is DataGrid)
            {
                DataGrid dataGrid = sender as DataGrid;
                if (dataGrid.SelectedItem != null && dataGrid.SelectedItem is ContactWithSingleDevice && NewMeeting != null)
                {
                    if (NewMeeting.Members == null)
                    {
                        NewMeeting.Members = new ObservableCollection<ContactWithSingleDevice>();

                    }
                    if (NewMeeting.Members.Contains(dataGrid.SelectedItem as ContactWithSingleDevice))
                    {
                        NewMeeting.Members.Remove(dataGrid.SelectedItem as ContactWithSingleDevice);
                        _viewModel.NotifyChange("UserMeetings");
                    }
                }
            }
        }
        private async void Single_Click(object sender, RoutedEventArgs e)
        {
            if (ChosenMeetingContacts_grid.SelectedItem == null) return;
            ContactWithSingleDevice cwd = ChosenMeetingContacts_grid.SelectedItem as ContactWithSingleDevice;
            await DataService.GetInstance().MakeCall(_viewModel.User.UserConfig.Extno1, cwd.Device.Number);
        }
        private CustomDialog CustomDialogMeeting { get; set; }
        private async void CustomDialogMeetingClose(object sender, RoutedEventArgs e)
        {
            if (ChosenMeeting == null) return;
            if (ChosenMeeting.Members == null || ChosenMeeting.Members.Count == 0) return;
            
            await DataService.GetInstance().HandupExt(_viewModel.User.UserConfig.Extno1);
            ChosenMeeting.Members.ToList().ForEach(async cwsd =>
            {
                if (cwsd.Contact.Status != null && cwsd.Contact.Status.Equals("通话中"))
                {
                    bool result = await DataService.GetInstance().HandupExt(cwsd.Device.Number);
                    if (result)
                    {
                        _viewModel.Message = cwsd.Device.Number + "挂断成功";
                    }
                    else
                    {
                        _viewModel.Message = cwsd.Device.Number + "挂断失败";
                    }
                }
            });
            await ((MainWindow)Application.Current.MainWindow).HideMetroDialogAsync(CustomDialogMeeting);
        }
        private async void GroupCall_Click(object sender, RoutedEventArgs e)
        {
            if (Meetings_DataGrid.SelectedItem == null) return;
            ChosenMeeting = (Meetings_DataGrid.SelectedItem as Models.Meeting);
            if (ChosenMeeting == null)
            {
                _viewModel.Message = "请首先选择一个会议。";
                return;
            }

            MainWindow mainWindow = Application.Current.MainWindow as MainWindow;

            if (CustomDialogMeeting == null)
            {
                CustomDialogMeeting = new CustomDialog(mainWindow.MetroDialogOptions)
                {
                    Content = this.Resources["CustomDialogMeeting"],
                    DialogContentMargin = new GridLength(0),
                    Width = 600,
                    BorderThickness = new Thickness(0),
                    BorderBrush = Brushes.DarkGray,
                    Background = new SolidColorBrush(Color.FromRgb(16, 57, 77)),//#10394d
                    DialogTop = this.Resources["CustomDialogMeetingClose"]
                };
                Grid topGrid = CustomDialogMeeting.DialogTop as Grid;
                topGrid.FindChild<Label>().SetCurrentValue(ContentProperty, "会议");
                (topGrid.Children[2] as Button).Click += CustomDialogMeetingClose;
            }
            else
            {
                Grid topGrid = CustomDialogMeeting.DialogTop as Grid;
                topGrid.FindChild<Label>().SetCurrentValue(ContentProperty, "会议");
            }
            await mainWindow.ShowMetroDialogAsync(CustomDialogMeeting);

            bool isgoing = ChosenMeeting.Members.ToList().Find(x => x.Contact.Status != null && x.Contact.Status.Equals("通话中")) != null;


            if (isgoing)
            {
                this.CustomDialogMeeting.FindChild<Button>("Start_List_Meeting_Button").SetCurrentValue(IsEnabledProperty, false);
                this.CustomDialogMeeting.FindChild<Button>("Invite_To_List_Meeting_Button").SetCurrentValue(IsEnabledProperty, true);
                this.CustomDialogMeeting.FindChild<Button>("Remove_From_List_Meeting_Button").SetCurrentValue(IsEnabledProperty, true);
                this.CustomDialogMeeting.FindChild<Button>("Stop_List_Meeting_Button").SetCurrentValue(IsEnabledProperty, true);
            }
            else
            {
                this.CustomDialogMeeting.FindChild<Button>("Start_List_Meeting_Button").SetCurrentValue(IsEnabledProperty, true);
                this.CustomDialogMeeting.FindChild<Button>("Invite_To_List_Meeting_Button").SetCurrentValue(IsEnabledProperty, false);
                this.CustomDialogMeeting.FindChild<Button>("Remove_From_List_Meeting_Button").SetCurrentValue(IsEnabledProperty, false);
                this.CustomDialogMeeting.FindChild<Button>("Stop_List_Meeting_Button").SetCurrentValue(IsEnabledProperty, false);
            }
        }

        private void MeetingsGrid_DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Meetings_DataGrid.SelectedItem != null)
            {
                ChosenMeeting = Meetings_DataGrid.SelectedItem as Meeting;
                _viewModel.NotifyChange("UserMeetings");
            }
        }

        private async void Save_UserMeetings(object sender, RoutedEventArgs e)
        {
            save();
            await ((MainWindow)Application.Current.MainWindow).HideMetroDialogAsync(CustomDialogAdd);
        }

        private async void Invite_To_List_Meeting(object sender, RoutedEventArgs e)
        {
            if (ChosenMeeting == null) return;
            DataGrid dataGrid = this.CustomDialogMeeting.FindChild<DataGrid>("MeetingMembersGrid");
            if (dataGrid.SelectedItem != null)
            {
                this.CustomDialogMeeting.FindChild<Button>("Invite_To_List_Meeting_Button").SetCurrentValue(IsEnabledProperty, false);
                ContactWithSingleDevice cwsd = dataGrid.SelectedItem as ContactWithSingleDevice;
                if (cwsd.Contact.Status == null || !cwsd.Contact.Status.Equals("通话中"))
                {
                    bool result = await DataService.GetInstance().MakeCall(cwsd.Device.Number, ChosenMeeting.Number);
                    if (result)
                    {
                        _viewModel.Message = cwsd.Device.Number + "成功";
                    }
                    else
                    {
                        _viewModel.Message = cwsd.Device.Number + "失败";
                    }
                }

                this.CustomDialogMeeting.FindChild<Button>("Invite_To_List_Meeting_Button").SetCurrentValue(IsEnabledProperty, true);
            }
        }

        private async void Remove_From_List_Meeting(object sender, RoutedEventArgs e)
        {
            DataGrid dataGrid = this.CustomDialogMeeting.FindChild<DataGrid>("MeetingMembersGrid");
            if (dataGrid.SelectedItem != null)
            {
                this.CustomDialogMeeting.FindChild<Button>("Remove_From_List_Meeting_Button").SetCurrentValue(IsEnabledProperty, false);
                ContactWithSingleDevice cwsd = dataGrid.SelectedItem as ContactWithSingleDevice;
                if (cwsd.Contact.Status != null && cwsd.Contact.Status.Equals("通话中"))
                {
                    bool result = await DataService.GetInstance().HandupExt(cwsd.Device.Number);
                    if (result)
                    {
                        _viewModel.Message = cwsd.Device.Number + "挂断成功";
                    }
                    else
                    {
                        _viewModel.Message = cwsd.Device.Number + "挂断失败";
                    }
                }

                this.CustomDialogMeeting.FindChild<Button>("Remove_From_List_Meeting_Button").SetCurrentValue(IsEnabledProperty, true);
            }

        }

        private async void Stop_List_Meeting(object sender, RoutedEventArgs e)
        {
            if (ChosenMeeting == null) return;
            if (ChosenMeeting.Members == null || ChosenMeeting.Members.Count == 0) return;
            this.CustomDialogMeeting.FindChild<Button>("Stop_List_Meeting_Button").SetCurrentValue(IsEnabledProperty, false);
            await DataService.GetInstance().HandupExt(_viewModel.User.UserConfig.Extno1);
            ChosenMeeting.Members.ToList().ForEach(async cwsd =>
            {
                if (cwsd.Contact.Status != null && cwsd.Contact.Status.Equals("通话中"))
                {
                    bool result = await DataService.GetInstance().HandupExt(cwsd.Device.Number);
                    if (result)
                    {
                        _viewModel.Message = cwsd.Device.Number + "挂断成功";
                    }
                    else
                    {
                        _viewModel.Message = cwsd.Device.Number + "挂断失败";
                    }
                }
            });

            this.CustomDialogMeeting.FindChild<Button>("Stop_List_Meeting_Button").SetCurrentValue(IsEnabledProperty, true);
            await ((MainWindow)Application.Current.MainWindow).HideMetroDialogAsync(CustomDialogMeeting);
        }

        private void MeetingContactDeviceListSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _viewModel.DialNo = ((sender as DataGrid).SelectedItem as Contact)?.Devices[0].Number;
        }

        private void Meeting_Contact_Search_Button(object sender, RoutedEventArgs e)
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
            _viewModel.NotifyChange("UserMeetings");
        }

        private async void Start_Meeting(object sender, RoutedEventArgs e)
        {
            if (ChosenMeeting == null) return;
            await DataService.GetInstance().MakeCall(_viewModel.User.UserConfig.Extno1, ChosenMeeting.Number);
            foreach (ContactWithSingleDevice cwsd in ChosenMeeting.Members)
            {
                await DataService.GetInstance().MakeCall(cwsd.Device.Number, ChosenMeeting.Number);
            }
            this.CustomDialogMeeting.FindChild<Button>("Start_List_Meeting_Button").SetCurrentValue(IsEnabledProperty, false);
            this.CustomDialogMeeting.FindChild<Button>("Invite_To_List_Meeting_Button").SetCurrentValue(IsEnabledProperty, true);
            this.CustomDialogMeeting.FindChild<Button>("Remove_From_List_Meeting_Button").SetCurrentValue(IsEnabledProperty, true);
            this.CustomDialogMeeting.FindChild<Button>("Stop_List_Meeting_Button").SetCurrentValue(IsEnabledProperty, true);
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
            _viewModel.NotifyChange("UserMeetings");
        }
    }
}
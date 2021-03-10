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
    /// UserPlans.xaml 的交互逻辑
    /// </summary>
    public partial class UserPlans : UserControl
    {
        private MainWindowViewModel _viewModel = null;
        public UserPlans()
        {
            InitializeComponent();
        }

        public Plan NewPlan { get; set; }
        public Plan ChosenPlan { get; set; }
        public ContactWithSingleDevice ChosenContact { get; set; }
        public List<string> PlanTypes { get; set; }
        public string ContactSearchText { get; set; }

        public bool IsOnlyFavorite { get; set; }
        public ObservableCollection<Contact> Contacts { get; set; }
        private void Control_Loaded(object sender, RoutedEventArgs e)
        {
            _viewModel = (DataContext as MainWindowViewModel);
            _viewModel.UserPlans = this;
            _viewModel.ProcessAtControl = this.ProcessAtControl;
        }

        private void ProcessAtControl(params object[] values)
        {
            ChosenPlanContacts_grid.Items.Refresh();
            if (CustomDialogMeeting != null)
            {
                this.CustomDialogMeeting.FindChild<DataGrid>("MeetingMembersGrid")?.Items.Refresh();
            }
        }

        private void Control_Unloaded(object sender, RoutedEventArgs e)
        {
            _viewModel.ProcessAtControl = null;
            _viewModel.UserPlans = null;
        }

        private CustomDialog CustomDialogAdd { get; set; }

        private async void CustomDialogAddClose(object sender, RoutedEventArgs e)
        {
            await ((MainWindow)Application.Current.MainWindow).HideMetroDialogAsync(CustomDialogAdd);
        }
        private async void Add_Click(object sender, RoutedEventArgs e)
        {
            NewPlan = new Plan();
            NewPlan.Members = new ObservableCollection<ContactWithSingleDevice>();

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
                topGrid.FindChild<Label>().SetCurrentValue(ContentProperty, "新增预案");
                (topGrid.Children[2] as Button).Click += CustomDialogAddClose;
            }
            else
            {
                Grid topGrid = CustomDialogAdd.DialogTop as Grid;
                topGrid.FindChild<Label>().SetCurrentValue(ContentProperty, "新增预案");
            }

            await mainWindow.ShowMetroDialogAsync(CustomDialogAdd);
        }

        private async void Edit_Click(object sender, RoutedEventArgs e)
        {
            if (ChosenPlan == null)
            {
                _viewModel.Message = "请选择一个预案";
                return;
            }
            NewPlan = ChosenPlan;

            _viewModel.NotifyChange("UserPlans");
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
            if (Plans_DataGrid.SelectedItem == null)
            {
                _viewModel.Message = "请选择一个预案";
                return;
            }
            ChosenPlan = Plans_DataGrid.SelectedItem as Plan;

            FilterDefinition<BsonDocument> filter = Builders<BsonDocument>.Filter.Eq("name", ChosenPlan.Name);
            IMongoCollection<BsonDocument> mongoColleciton = DataService.GetInstance().Database.GetCollection<BsonDocument>("plans");
            mongoColleciton.DeleteOne(filter);
            _viewModel.Message = ChosenPlan.Name + "信息删除成功。";
            LoadPlan();
            ChosenPlan = null;
        }

        private async void save()
        {
            if (NewPlan == null||
                NewPlan.Name==null||
                NewPlan.Number==null||
                NewPlan.Members==null||
                NewPlan.Content==null||
               !Core.Utils.IsNumberInRange(NewPlan.Number, Models.SystemConfig.Instance.MeetingFrom, Models.SystemConfig.Instance.MeetingTo)) return;
            _viewModel.Message = "正在保存预案信息...";
            DataService.GetInstance().Database
                    .GetCollection<BsonDocument>("plans")
                    .DeleteOne(Builders<BsonDocument>.Filter.Eq("name", NewPlan.Name));
            BsonArray bsonArray = new BsonArray();
            foreach (ContactWithSingleDevice c in NewPlan.Members)
            {
                bsonArray.Add(c.Device.Number);
            }
            DataService.GetInstance().Database
                .GetCollection<BsonDocument>("plans")
                .InsertOne(new BsonDocument {
                        {"number",NewPlan.Number },
                        {"name", NewPlan.Name},
                        {"content",NewPlan.Content},
                        {"members",bsonArray }
                });
            _viewModel.Message += "成功";
            _viewModel.Plans = new ObservableCollection<Plan>(DataService.GetInstance().QueryPlans(_viewModel));
            _viewModel.NotifyChange("Plans");
        }


        private void ChosenPlanContacts_grid_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (ChosenPlanContacts_grid.SelectedItem != null)
            {
                ChosenContact = ChosenPlanContacts_grid.SelectedItem as ContactWithSingleDevice;
                _viewModel.DialNo = ChosenContact.Device.Number;
                _viewModel.NotifyChange("UserPlans");
            }

        }


        private void LoadPlan()
        {
            _viewModel.Plans = new ObservableCollection<Plan>(DataService.GetInstance().QueryPlans(_viewModel));
            _viewModel.NotifyChange("Plans");
        }

        private void Source_Contact_Grid_Double_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender != null && sender is DataGrid)
            {
                DataGrid dataGrid = sender as DataGrid;
                if (dataGrid.SelectedItem != null && dataGrid.SelectedItem is Contact && NewPlan != null)
                {
                    if (NewPlan.Members == null)
                    {
                        NewPlan.Members = new ObservableCollection<ContactWithSingleDevice>();

                    }
                    ContactWithSingleDevice cwsd = NewPlan.Members.ToList().Find(x => x.Contact.Name.Equals((dataGrid.SelectedItem as Contact).Name));
                    if (cwsd == null)
                    {
                        Contact c = dataGrid.SelectedItem as Contact;
                        NewPlan.Members.Add(new ContactWithSingleDevice
                        {
                            Contact = c,
                            Device = c.Devices[0]
                        });
                        _viewModel.NotifyChange("UserPlans");
                    }
                }
            }
        }

        private void Dest_Contact_Grid_Double_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender != null && sender is DataGrid)
            {
                DataGrid dataGrid = sender as DataGrid;
                if (dataGrid.SelectedItem != null && dataGrid.SelectedItem is ContactWithSingleDevice && NewPlan != null)
                {
                    if (NewPlan.Members == null)
                    {
                        NewPlan.Members = new ObservableCollection<ContactWithSingleDevice>();

                    }
                    if (NewPlan.Members.Contains(dataGrid.SelectedItem as ContactWithSingleDevice))
                    {
                        NewPlan.Members.Remove(dataGrid.SelectedItem as ContactWithSingleDevice);
                        _viewModel.NotifyChange("UserPlans");
                    }
                }
            }
        }
        private async void Single_Click(object sender, RoutedEventArgs e)
        {
            if (ChosenPlanContacts_grid.SelectedItem == null) return;
            ContactWithSingleDevice cwd = ChosenPlanContacts_grid.SelectedItem as ContactWithSingleDevice;
            await DataService.GetInstance().MakeCall(_viewModel.User.UserConfig.Extno1, cwd.Device.Number);
        }
        private CustomDialog CustomDialogMeeting { get; set; }
        private async void CustomDialogMeetingClose(object sender, RoutedEventArgs e)
        {
            if (ChosenPlan == null) return;
            if (ChosenPlan.Members == null || ChosenPlan.Members.Count == 0) return;
            this.CustomDialogMeeting.FindChild<Button>("Stop_List_Meeting_Button").SetCurrentValue(IsEnabledProperty, false);
            await DataService.GetInstance().HandupExt(_viewModel.User.UserConfig.Extno1);
            ChosenPlan.Members.ToList().ForEach(async cwsd =>
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
        private async void GroupCall_Click(object sender, RoutedEventArgs e)
        {
            if (Plans_DataGrid.SelectedItem == null) return;
            ChosenPlan = (Plans_DataGrid.SelectedItem as Models.Plan);
            if (ChosenPlan == null)
            {
                _viewModel.Message = "请首先选择一个预案。";
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

            bool isgoing = ChosenPlan.Members.ToList().Find(x => x.Contact.Status != null && x.Contact.Status.Equals("通话中")) != null;


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

        private void PlansGrid_DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Plans_DataGrid.SelectedItem != null)
            {
                ChosenPlan = Plans_DataGrid.SelectedItem as Plan;
                _viewModel.NotifyChange("UserPlans");
            }
        }

        private async void Save_UserPlans(object sender, RoutedEventArgs e)
        {
            save();
            await ((MainWindow)Application.Current.MainWindow).HideMetroDialogAsync(CustomDialogAdd);
        }

        private async void Invite_To_List_Meeting(object sender, RoutedEventArgs e)
        {
            if (ChosenPlan == null) return;
            DataGrid dataGrid = this.CustomDialogMeeting.FindChild<DataGrid>("MeetingMembersGrid");
            if (dataGrid.SelectedItem != null)
            {
                this.CustomDialogMeeting.FindChild<Button>("Invite_To_List_Meeting_Button").SetCurrentValue(IsEnabledProperty, false);
                ContactWithSingleDevice cwsd = dataGrid.SelectedItem as ContactWithSingleDevice;
                if (cwsd.Contact.Status == null || !cwsd.Contact.Status.Equals("通话中"))
                {
                    bool result = await DataService.GetInstance().MakeCall(cwsd.Device.Number, ChosenPlan.Number);
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
            if (ChosenPlan == null) return;
            if (ChosenPlan.Members == null || ChosenPlan.Members.Count == 0) return;
            this.CustomDialogMeeting.FindChild<Button>("Stop_List_Meeting_Button").SetCurrentValue(IsEnabledProperty, false);
            await DataService.GetInstance().HandupExt(_viewModel.User.UserConfig.Extno1);
            ChosenPlan.Members.ToList().ForEach(async cwsd =>
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
            _viewModel.NotifyChange("UserPlans");
        }

        private async void Start_Meeting(object sender, RoutedEventArgs e)
        {
            if (ChosenPlan == null) return;
            await DataService.GetInstance().MakeCall(_viewModel.User.UserConfig.Extno1, ChosenPlan.Number);
            foreach (ContactWithSingleDevice cwsd in ChosenPlan.Members)
            {
                await DataService.GetInstance().MakeCall(cwsd.Device.Number, ChosenPlan.Number);
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
            _viewModel.NotifyChange("UserPlans");
        }
    }
}

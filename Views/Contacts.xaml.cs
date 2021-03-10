using Arco.Models;
using Arco.Models.Org;
using Arco.Services;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro.IconPacks;
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
    /// Contacts.xaml 的交互逻辑
    /// </summary>
    public partial class Contacts : UserControl
    {
        private MainWindowViewModel _viewModel = null;
        public Contacts()
        {
            InitializeComponent();

        }
        public ObservableCollection<Orgnization> Orgnizations { get; set; }
        public Contact ChosenContact { get; set; }
        public Contact NewContact { get; set; }

        public ObservableCollection<Models.CallHistory> CallHistory { get; set; }
        private void Contact_Loaded(object sender, RoutedEventArgs e)
        {
            _viewModel = (DataContext as MainWindowViewModel);
            _viewModel.ViewContacts = this;
            LoadMyTree(_viewModel.BaseContacts);
        }

        private void BuildupDepartmentChildren(Department department, TreeViewItem treeViewItem)
        {

            if (_viewModel.Departments.ToList().FindAll(x => x.Father.Equals(department.Name)).Count == 0) return;
            _viewModel.Departments.ToList().FindAll(x => x.Father.Equals(department.Name)).ForEach(dept =>
            {
                TreeViewItem treeViewItemDepartment = GetTreeView("D" + dept.Id.ToString(), dept.Name, new PackIconTypicons
                {
                    Kind = PackIconTypiconsKind.Folder
                });

                treeViewItem.Items.Add(treeViewItemDepartment);
                treeViewItemDepartment.IsExpanded = false;
                BuildupDepartmentChildren(dept, treeViewItemDepartment);
                _viewModel.BaseContacts.FindAll(c => c.Department.Name.Equals(dept.Name)).ForEach(con =>
                {
                    treeViewItemDepartment.Items.Add(GetTreeView("C" + con.Id.ToString(), con.Name, new PackIconIonicons { Kind = PackIconIoniconsKind.PersonMD }));
                });

            });
        }

        public void LoadMyTree(List<Contact> contacts)
        {
            if (contacts == null || contacts.Count == 0) return;
            Department topDepartment = _viewModel.Departments.ToList().Find(x => x.Father.Equals(""));
            if (topDepartment == null) return;
            TreeViewItem topDepartmentTreeViewItem = GetTreeView("D" + topDepartment.Id.ToString(), topDepartment.Name, new PackIconTypicons
            {
                Kind = PackIconTypiconsKind.Folder
            });
            topDepartmentTreeViewItem.IsExpanded = true;
            BuildupDepartmentChildren(topDepartment, topDepartmentTreeViewItem);
            contacts.FindAll(c => c.Department.Name.Equals(topDepartment.Name))
                .ForEach(x =>
                {
                    topDepartmentTreeViewItem.Items.Add(GetTreeView("C" + x.Id.ToString(), x.Name + (x.IsFavorite ? " - 已收藏" : ""), new PackIconIonicons { Kind = PackIconIoniconsKind.PersonMD }));
                });
            ContactTree.Items.Clear();
            ContactTree.Items.Add(topDepartmentTreeViewItem);
        }


        private TreeViewItem GetTreeView(string uid, string text, Control icon)
        {
            TreeViewItem item = new TreeViewItem();
            item.Uid = uid;
            item.IsExpanded = true;

            StackPanel stack = new StackPanel();
            
            stack.Orientation = Orientation.Horizontal;
            icon.SetCurrentValue(MarginProperty, new Thickness(5));
            icon.SetCurrentValue(WidthProperty, (double)26);
            icon.SetCurrentValue(HeightProperty, (double)26);
            icon.SetCurrentValue(ForegroundProperty, Brushes.White);
            stack.Children.Add(icon);

            stack.Children.Add(new Label { Content = text, Margin = new Thickness(5), FontSize = 14, Foreground=Brushes.Black });
            item.Header = stack;
            return item;
        }

        private async void Edit_Click(object sender, RoutedEventArgs e)
        {
            if (ChosenContact == null)
            {
                _viewModel.Message = "请选择一个联系人。";
                return;
            }
            NewContact = ChosenContact;
            NewContact.Position = new List<Models.Position>(_viewModel.Positions).Find(x => x.Name.Equals(NewContact.Position.Name));
            NewContact.Department = new List<Models.Department>(_viewModel.Departments).Find(x => x.Name.Equals(NewContact.Department.Name));
            _viewModel.NotifyChange("ViewContacts");
            MainWindow mainWindow = Application.Current.MainWindow as MainWindow;

            if (CustomDialogAddContract == null)
            {
                CustomDialogAddContract = new CustomDialog(mainWindow.MetroDialogOptions)
                {
                    Content = this.Resources["CustomDialogAddContract"],
                    DialogContentMargin = new GridLength(0),
                    Width = 600,
                    BorderThickness = new Thickness(0),
                    BorderBrush = Brushes.DarkGray,
                    Background = new SolidColorBrush(Color.FromRgb(16, 57, 77)),//#10394d
                    DialogTop = this.Resources["CustomDialogAddContractClose"]
                };
                Grid topGrid = CustomDialogAddContract.DialogTop as Grid;
                topGrid.FindChild<Label>().SetCurrentValue(ContentProperty, "修改联系人");
                (topGrid.Children[2] as Button).Click += CustomDialogAddContractClose;
            }
            else
            {
                Grid topGrid = CustomDialogAddContract.DialogTop as Grid;
                topGrid.FindChild<Label>().SetCurrentValue(ContentProperty, "修改联系人");
            }
            await mainWindow.ShowMetroDialogAsync(CustomDialogAddContract);
            this.CustomDialogAddContract.FindChild<TextBox>("NewContact_Name").SetCurrentValue(IsEnabledProperty, false);
        }

        private void Del_Click(object sender, RoutedEventArgs e)
        {
            if (ChosenContact == null)
            {
                _viewModel.Message = "请选择一个联系人。";
                return;
            }
            FilterDefinition<BsonDocument> filter = Builders<BsonDocument>.Filter.Eq("Name", ChosenContact.Name);
            IMongoCollection<BsonDocument> mongoColleciton = DataService.GetInstance().Database.GetCollection<BsonDocument>("Contact");
            mongoColleciton.DeleteOne(filter);
            _viewModel.Message = ChosenContact.Name + "信息删除成功。";
            ChosenContact = null;
            _viewModel.UpdateData();
            LoadMyTree(_viewModel.BaseContacts);
        }


        private async void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            object selectedItem = (sender as TreeView).SelectedItem;
            if (selectedItem != null && selectedItem is TreeViewItem && ((TreeViewItem)selectedItem).Uid.StartsWith("C"))
            {
                string oid = ((TreeViewItem)selectedItem).Uid.Substring(1);
                ChosenContact = _viewModel.BaseContacts.Find(x => x.Id.ToString().Equals(oid));
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

                //Cdr_Grid.Items.Refresh();
                _viewModel.NotifyChange("ViewContacts");
            }
        }


        private async void Save_Contact(object sender, RoutedEventArgs e)
        {
            if (NewContact == null) return;
            if (NewContact.Name == null ||
                NewContact.Devices[0].Number == null ||
                NewContact.Position.Name == null ||
                NewContact.Department.Name == null) return;
            (sender as Button).SetCurrentValue(IsEnabledProperty, false);
            bool isnew = false;
            Contact c = new List<Contact>(_viewModel.BaseContacts).Find(x => x.Name.Equals(NewContact.Name));
            if (c == null)
            {
                c = NewContact;
                isnew = true;
            }

            c.Position = new List<Models.Position>(_viewModel.Positions).Find(x => x.Name.Equals(NewContact.Position.Name));
            c.Department = new List<Department>(_viewModel.Departments).Find(x => x.Name.Equals(NewContact.Department.Name));
            c.Devices[0].Number = NewContact.Devices[0].Number ?? "";
            c.Devices[1].Number = NewContact.Devices[1].Number ?? "";
            c.Devices[2].Number = NewContact.Devices[2].Number ?? "";

            IMongoCollection<BsonDocument> mongoColleciton = DataService.GetInstance().Database.GetCollection<BsonDocument>("Contact");
            if (isnew)
            {
                mongoColleciton.InsertOne(c.ToBsonDocument());
            }
            else
            {
                FilterDefinition<BsonDocument> filter = Builders<BsonDocument>.Filter.Eq("Name", c.Name);
                BsonArray bsonArray = new BsonArray();
                c.Devices.ForEach(x =>
                {
                    bsonArray.Add(x.ToBsonDocument());
                });
                UpdateDefinition<BsonDocument> update = Builders<BsonDocument>.Update
                    .Set("Position", c.Position.ToBsonDocument())
                    .Set("Devices", bsonArray)
                    .Set("IsFavorite", new BsonBoolean(c.IsFavorite))
                    .Set("Department", c.Department.ToBsonDocument());
                mongoColleciton.UpdateOne(filter, update);
            }
            _viewModel.Message = c.Name + "信息保存成功。";
            _viewModel.UpdateData();
            LoadMyTree(_viewModel.BaseContacts);
            NewContact = null;
            _viewModel.NotifyChange("ViewContacts");;
            (sender as Button).SetCurrentValue(IsEnabledProperty, true);
            await ((MainWindow)Application.Current.MainWindow).HideMetroDialogAsync(CustomDialogAddContract);
        }

        private CustomDialog CustomDialogAddContract { get; set; }
        private async void CustomDialogAddContractClose(object sender, RoutedEventArgs e)
        {
            await ((MainWindow)Application.Current.MainWindow).HideMetroDialogAsync(CustomDialogAddContract);
        }
        private async void Add_Click(object sender, RoutedEventArgs e)
        {
            NewContact = new Contact()
            {
                Position = new Models.Position(),
                Department = new Department(),
                Devices = new List<Device>(new Device[] { new Device()
                {
                    DeviceType = new DeviceType()
                    {
                        Name = "分机"
                    }
                },
                new Device()
                {
                    DeviceType = new DeviceType()
                    {
                        Name = "手机"
                    }
                },
                new Device()
                {
                    DeviceType = new DeviceType()
                    {
                        Name = "卫星电话"
                    }
                }

                })
            };

            MainWindow mainWindow = Application.Current.MainWindow as MainWindow;

            if (CustomDialogAddContract == null)
            {
                CustomDialogAddContract = new CustomDialog(mainWindow.MetroDialogOptions)
                {
                    Content = this.Resources["CustomDialogAddContract"],
                    DialogContentMargin = new GridLength(0),
                    Width = 600,
                    BorderThickness = new Thickness(0),
                    BorderBrush = Brushes.DarkGray,
                    Background = new SolidColorBrush(Color.FromRgb(16, 57, 77)),//#10394d
                    DialogTop = this.Resources["CustomDialogAddContractClose"]
                };
                Grid topGrid = CustomDialogAddContract.DialogTop as Grid;
                topGrid.FindChild<Label>().SetCurrentValue(ContentProperty, "新增联系人");
                (topGrid.Children[2] as Button).Click += CustomDialogAddContractClose;
            }
            else
            {
                Grid topGrid = CustomDialogAddContract.DialogTop as Grid;
                topGrid.FindChild<Label>().SetCurrentValue(ContentProperty, "新增联系人");
            }

            await mainWindow.ShowMetroDialogAsync(CustomDialogAddContract);
            this.CustomDialogAddContract.FindChild<TextBox>("NewContact_Name").SetCurrentValue(IsEnabledProperty, true);
        }

        public string ContactSearchText { get; set; }
        public bool IsOnlyFavorite { get; set; }

        private void Contact_Search_Button(object sender, RoutedEventArgs e)
        {
            LoadMyTree(new List<Contact>(_viewModel.BaseContacts).FindAll(x =>
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

            _viewModel.NotifyChange("ViewContacts");
        }

        private void Contact_Device_Grid_Selection_Changed(object sender, SelectionChangedEventArgs e)
        {
            _viewModel.DialNo = ((sender as DataGrid).SelectedItem as Device)?.Number;
        }

        private void Control_Unloaded(object sender, RoutedEventArgs e)
        {
            _viewModel.ProcessAtControl = null;
            _viewModel.ViewContacts = null;
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
    }
}

using Arco.Core;
using Arco.Models;
using Arco.Services;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro.IconPacks;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Arco.Views
{
    /// <summary>
    /// Home.xaml 的交互逻辑
    /// </summary>
    public partial class Home : UserControl
    {
        private MainWindowViewModel _viewModel = null;
        private ContactService contactService = new ContactService();
        ObservableCollection<Contact> contacts = new ObservableCollection<Contact>();
        public int ContactPage { set; get; }
        public int ContactTotalPages { get; set; }
        public Contact CurrentContact { get; set; }

        public ObservableCollection<Message> CurrentContactMessages { get; set; }
        public string ContactSearchText { get; set; }
        public bool IsOnlyInCall { set; get; }
        public bool IsOnlyFavorite { set; get; }
        public string DialNo { set; get; }
        public Home()
        {
            InitializeComponent();
            ContactPage = 1;
        }


        public void SetupCalls()
        {
            List<Message> _list = new List<Message>(contactService.Messages).FindAll(
                (x) =>
                {
                    bool isInvolved = false;
                    foreach (Device device in CurrentContact.Devices)
                    {
                        if (x.From.Equals(device.Number) || x.To.Equals(device.Number))
                        {
                            isInvolved = true;
                            break;
                        }
                    }
                    return isInvolved;

                });
            _list.Sort(delegate (Message x, Message y)
            {
                if (x.MessageTime.CompareTo(y.MessageTime) > 0)
                {
                    return -1;
                }

                return 1;
            });
            CurrentContactMessages = new ObservableCollection<Message>(_list);
        }
        public void UpdateContactTile(string contactid, bool isInCall)
        {
            Grid grid = this.FindName("tileGridContact") as Grid;
            foreach (DockPanel panel in grid.Children)
            {
                Tile tile = panel.Children[0] as Tile;
                if (tile.Name.Equals("tile_" + contactid))
                {
                    tile.Background = isInCall ? (new SolidColorBrush(Color.FromRgb(210, 105, 30))) : (new SolidColorBrush(Color.FromRgb(41, 142, 221)));
                    break;
                }
            }
        }

        private void UpdateCurrentPageContact(Contact contact)
        {
            if (tileGridContact.Children == null || tileGridContact.Children.Count == 0) return;
            
            int index = CurrentPageContacts.IndexOf(contact);
            if (index == -1) return;
            DockPanel dockPanel = tileGridContact.Children[index] as DockPanel;
            Tile tile = dockPanel.Children[0] as Tile;
            if (tile == null) return;
            switch (contact.Status)
            {
                case "响铃":
                    tile.Background = Brushes.Red;
                    break;
                case "通话中":
                    tile.Background = new SolidColorBrush(Color.FromRgb(0, 255, 0));
                    break;
                case "空闲":
                    tile.Background = new SolidColorBrush(Color.FromRgb(28, 173, 228));
                    break;
                case "已注册":
                    tile.Background = new SolidColorBrush(Color.FromRgb(28, 173, 228));
                    break;
                case "未注册":
                    tile.Background = new SolidColorBrush(Color.FromRgb(98, 163, 159));
                    break;

            }
        }
        private List<Contact> CurrentPageContacts { get; set; }

        private void ArrangeContact()
        {
            Grid grid = this.FindName("tileGridContact") as Grid;
            grid.Children.Clear();
            CurrentPageContacts = new List<Contact>();

            for (int i = (ContactPage - 1) * 40; i < ContactPage * 40 && i < contacts.Count; i++)
            {
                Contact contact = this.contacts[i];
                CurrentPageContacts.Add(contact);

                Tile tile = new Tile();

                tile.Name = "tile_" + contact.Id.ToString();
                tile.Click += Tile_Click;
                tile.HorizontalContentAlignment = HorizontalAlignment.Right;
                tile.VerticalContentAlignment = VerticalAlignment.Top;

                tile.Title = contact.Name + (contact.IsFavorite ? "(已收藏)" : "") +
                    "\n部门：" + contact.Department.Name +
                    "\n职务：" + contact.Position.Name +
                    "\n" + contact.Devices[0].DeviceType.Name + "：" + contact.Devices[0].Number +
                    "\n位置：" + contact.Point?.Longitude + " " + contact.Point?.Latitude;
                tile.Style = (Style)this.Resources["SmallTileStyle"];
                tile.FontSize = 14;
                PackIconMaterial icon = new PackIconMaterial();
                icon.Width = 30;
                icon.Height = 30;

                tile.Content = icon;

                if (contact.Status == null)
                {
                    contact.Status =  "未注册";
                }
                switch (contact.Status)
                {
                    case "响铃":
                        tile.Background = Brushes.Red;
                        break;
                    case "通话中":
                        tile.Background = new SolidColorBrush(Color.FromRgb(0, 255, 0));
                        break;
                    case "空闲":
                        tile.Background = new SolidColorBrush(Color.FromRgb(28, 173, 228));
                        break;
                    case "已注册":
                        tile.Background = new SolidColorBrush(Color.FromRgb(28, 173, 228));
                        break;
                    case "未注册":
                        tile.Background = new SolidColorBrush(Color.FromRgb(98, 163, 159));
                        break;

                }
                icon.Kind = PackIconMaterialKind.AccountCheckOutline;
                DockPanel dockPanel = new DockPanel();
                dockPanel.Children.Add(tile);
                Grid.SetColumn(dockPanel, i % 8);
                Grid.SetRow(dockPanel, (int)(i % 40 / 8));
                grid.Children.Add(dockPanel);
            }
        }

        private void Tile_Click(object sender, RoutedEventArgs e)
        {
            Tile tile = sender as Tile;
            if (this.CurrentContact != null)
            {
                foreach (UIElement uie in (this.FindName("tileGridContact") as Grid).Children)
                {
                    uie.FindChild<Tile>().SetCurrentValue(BorderThicknessProperty, new Thickness(0));
                }
            }
            string contactid = tile.Name.Substring("tile_".Length);
            this.CurrentContact = new List<Contact>(contacts).Find(x => x.Id.ToString().Equals(contactid));
            this.SetupCalls();
            tile.BorderBrush = Brushes.DarkOrange;
            tile.BorderThickness = new Thickness(4);
            _viewModel.NotifyChange("Home");
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            _viewModel = (DataContext as MainWindowViewModel);
            _viewModel.Home = this;
            _viewModel.ProcessAtControl = this.ProcessAtControl;
            if (_viewModel.BaseContacts.Count == 0) return;
            contacts = new ObservableCollection<Contact>(_viewModel.BaseContacts);

            ContactPage = 1;
            ContactTotalPages = (contacts.Count % 40 == 0 ? 0 : 1) + (int)(contacts.Count / 40);
            CurrentContact = contacts[0];
            this.SetupCalls();

            IsOnlyInCall = false;

            ArrangeContact();

            _viewModel.NotifyChange("Home");
        }

        private void Next_Page(object sender, RoutedEventArgs e)
        {
            if (ContactPage < ContactTotalPages)
            {
                ContactPage = ContactPage + 1;
            }
            this.ArrangeContact();
            _viewModel.NotifyChange("Home");
        }
        private void Pre_Page(object sender, RoutedEventArgs e)
        {
            if (ContactPage > 1)
            {
                ContactPage = ContactPage - 1;
            }
            this.ArrangeContact();
            _viewModel.NotifyChange("Home");
        }


        private void Contact_Query(object sender, RoutedEventArgs e)
        {
            List<Contact> result = new List<Contact>(_viewModel.BaseContacts).FindAll(
                (x) =>
                {
                    if (!IsOnlyFavorite)
                    {
                        if (this.IsOnlyInCall)
                        {
                            return x.ConversationId != null && x.ConversationId.Length > 0 &&
                                (this.ContactSearchText == null || (x.Name.IndexOf(this.ContactSearchText) != -1 ||
                                x.Department.Name.IndexOf(this.ContactSearchText) != -1 ||
                                x.Position.Name.IndexOf(this.ContactSearchText) != -1
                                ));
                        }
                        else
                        {
                            return this.ContactSearchText == null || (x.Name.IndexOf(this.ContactSearchText) != -1 ||
                               x.Department.Name.IndexOf(this.ContactSearchText) != -1 ||
                               x.Position.Name.IndexOf(this.ContactSearchText) != -1
                               );
                        }
                    }
                    else
                    {
                        if (this.IsOnlyInCall)
                        {
                            return x.IsFavorite && (x.ConversationId != null && x.ConversationId.Length > 0 &&
                                (this.ContactSearchText == null || (x.Name.IndexOf(this.ContactSearchText) != -1 ||
                                x.Department.Name.IndexOf(this.ContactSearchText) != -1 ||
                                x.Position.Name.IndexOf(this.ContactSearchText) != -1
                                )));
                        }
                        else
                        {
                            return x.IsFavorite && (this.ContactSearchText == null || (x.Name.IndexOf(this.ContactSearchText) != -1 ||
                               x.Department.Name.IndexOf(this.ContactSearchText) != -1 ||
                               x.Position.Name.IndexOf(this.ContactSearchText) != -1
                               ));
                        }
                    }
                }
                );
            contacts = new ObservableCollection<Contact>(result);
            ContactTotalPages = (int)contacts.Count / 40 + ((contacts.Count % 40 == 0) ? 0 : 1);
            ContactPage = 1;
            _viewModel.NotifyChange("Home");
            ArrangeContact();
        }

        private void Contact_Go_Page(object sender, RoutedEventArgs e)
        {
            ArrangeContact();
            _viewModel.NotifyChange("Home");
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            _viewModel.ProcessAtControl = null;
            _viewModel.Home = null;
        }

        private void CallList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            object obj = (sender as DataGrid).SelectedItem;
            if (obj != null)
            {
                _viewModel.ChosenCall = obj as Call;
                this.Disconnnect_Button.SetCurrentValue(IsEnabledProperty, true);
                this.Listen_Button.SetCurrentValue(IsEnabledProperty, true);
                this.Insert_Button.SetCurrentValue(IsEnabledProperty, true);
            }
        }

        private async void Disconnnect_Button_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.ChosenCall != null)
            {
                _viewModel.Message = "强拆话路：" + _viewModel.ChosenCall.Caller + " -> " + _viewModel.ChosenCall.Callee + "...";
                string json = null;
                string url = null;
                if (Convert.ToInt32(_viewModel.ChosenCall.Caller) >= Models.SystemConfig.Instance.ExtFrom &&
                    Convert.ToInt32(_viewModel.ChosenCall.Caller) <= Models.SystemConfig.Instance.ExtTo)
                {
                    json = "{\"extid\": \"" + _viewModel.ChosenCall.Caller + "\"}";
                    url = "/extension/hangup";
                }

                if (Convert.ToInt32(_viewModel.ChosenCall.Callee) >= Models.SystemConfig.Instance.ExtFrom &&
                    Convert.ToInt32(_viewModel.ChosenCall.Callee) <= Models.SystemConfig.Instance.ExtTo


                   )
                {
                    json = "{\"extid\": \"" + _viewModel.ChosenCall.Callee + "\"}";
                    url = "/extension/hangup";
                }

                if (json == null)
                {
                    if (_viewModel.ChosenCall.Direction.Equals("呼入"))
                    {
                        json = "{\"inboundid\": \"" + _viewModel.ChosenCall.CallerChannelId + "\"}";
                        url = "/inbound/hangup";
                    }
                    else
                    {
                        json = "{\"outboundid\": \"" + _viewModel.ChosenCall.CallerChannelId + "\"}";
                        url = "/outbound/hangup";

                    }
                }

                JObject response = await DataService.GetInstance().PostAsync(json, url, true);
                if (response != null)
                {
                    if (((string)response["status"]).Equals("Success"))
                    {
                        _viewModel.Message += "成功";
                        _viewModel.ChosenCall = null;
                        this.Disconnnect_Button.SetCurrentValue(IsEnabledProperty, false);
                        this.Listen_Button.SetCurrentValue(IsEnabledProperty, false);
                        this.Insert_Button.SetCurrentValue(IsEnabledProperty, false);
                    }
                    else
                    {
                        _viewModel.Message += "失败";
                    }
                }

            }
        }

        private async void Listen_Button_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.ChosenCall != null)
            {
                if (_viewModel.ChosenCall.CallId.Equals(_viewModel.ExtNoCall.CallId))
                {
                    _viewModel.Message = "本机话路，已在话路中";
                    return;
                }
                _viewModel.Message = "监听话路：" + _viewModel.ChosenCall.Caller + " -> " + _viewModel.ChosenCall.Callee + "...";
                string listenedExt = null;
                if (Convert.ToInt32(_viewModel.ChosenCall.Caller) >= Models.SystemConfig.Instance.ExtFrom &&
                    Convert.ToInt32(_viewModel.ChosenCall.Caller) <= Models.SystemConfig.Instance.ExtTo)
                {
                    listenedExt = _viewModel.ChosenCall.Caller;
                }
                if (Convert.ToInt32(_viewModel.ChosenCall.Callee) >= Models.SystemConfig.Instance.ExtFrom &&
                    Convert.ToInt32(_viewModel.ChosenCall.Callee) <= Models.SystemConfig.Instance.ExtTo)
                {
                    listenedExt = _viewModel.ChosenCall.Callee;
                }
                if (listenedExt == null)
                {
                    _viewModel.Message = "话路监听失败。";
                    return;
                }

                string json = "{" + "\"listener\": \"" + _viewModel.User.UserConfig.Extno1 + "\"," + "\"listenedext\":\"" + listenedExt + "\"}";
                JObject response = await DataService.GetInstance().PostAsync(json, "/extension/listen", true);
                if (response != null)
                {
                    if (((string)response["status"]).Equals("Success"))
                    {
                        _viewModel.Message += "成功";
                        _viewModel.ChosenCall = null;
                        this.Disconnnect_Button.SetCurrentValue(IsEnabledProperty, false);
                        this.Listen_Button.SetCurrentValue(IsEnabledProperty, false);
                        this.Insert_Button.SetCurrentValue(IsEnabledProperty, false);
                    }
                    else
                    {
                        _viewModel.Message += "失败";
                    }
                }

            }
        }

        private async void Insert_Button_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.ChosenCall != null)
            {
                if (_viewModel.ExtNoCall!=null && _viewModel.ChosenCall.CallId.Equals(_viewModel.ExtNoCall.CallId))
                {
                    _viewModel.Message = "本机话路，已在话路中";
                    return;
                }
                string listenedExt = null;
                if (Convert.ToInt32(_viewModel.ChosenCall.Caller) >= Models.SystemConfig.Instance.ExtFrom &&
                    Convert.ToInt32(_viewModel.ChosenCall.Caller) <= Models.SystemConfig.Instance.ExtTo)
                {
                    listenedExt = _viewModel.ChosenCall.Caller;
                }
                if (Convert.ToInt32(_viewModel.ChosenCall.Callee) >= Models.SystemConfig.Instance.ExtFrom &&
                    Convert.ToInt32(_viewModel.ChosenCall.Callee) <= Models.SystemConfig.Instance.ExtTo)
                {
                    listenedExt = _viewModel.ChosenCall.Callee;
                }
                if (listenedExt == null)
                {
                    _viewModel.Message = "话路监听失败。";
                    return;
                }
                _viewModel.Message = "强插话路：" + _viewModel.ChosenCall.Caller + " -> " + _viewModel.ChosenCall.Callee + "...";
                string json = "{" + "\"bargein\": \"" + _viewModel.DialNo + "\"," + "\"bargedext\": \"" + listenedExt + "\"" + "}";
                JObject response = await DataService.GetInstance().PostAsync(json, "/extension/barge", true);
                if (response != null)
                {
                    if (((string)response["status"]).Equals("Success"))
                    {
                        _viewModel.Message += "成功";
                        _viewModel.ChosenCall = null;
                        this.Disconnnect_Button.SetCurrentValue(IsEnabledProperty, false);
                        this.Listen_Button.SetCurrentValue(IsEnabledProperty, false);
                        this.Insert_Button.SetCurrentValue(IsEnabledProperty, false);
                    }
                    else
                    {
                        _viewModel.Message += "失败";
                    }
                }

            }
        }

        private void DeviceList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DeviceList.SelectedItem != null)
            {
                _viewModel.DialNo = (DeviceList.SelectedItem as Device).Number;
            }
        }

        private void ProcessAtControl(params object[] values)
        {
            JObject result = values[0] as JObject;
            string action = (string)result["action"];
            if (action.Equals("ExtensionStatus"))
            {
                this.UpdateCurrentPageContact(values[1] as Contact);
                _viewModel.NotifyChange("Home");
                DeviceList.Items.Refresh();
                return;
            }


            if (action.Equals("ANSWER") || action.Equals("ANSWERED"))
            {
                string callid = (string)result["callid"];
                Call existCall = new List<Call>(_viewModel.Calls).Find((x) => { return x.CallId.Equals(callid); });
                List<Contact> cs = new List<Contact>(_viewModel.BaseContacts).FindAll((x) =>
                {
                    return x.Devices[0].Number.Equals(existCall.Caller) || x.Devices[0].Number.Equals(existCall.Callee);
                });
                if (cs != null)
                {
                    cs.ForEach((c) =>
                    {
                        c.ConversationId = existCall.CallId;
                        //UpdateContactTile(c.Id.ToString(), true);
                    });
                }
                
                return;
            }
           
            
        }

    }
}

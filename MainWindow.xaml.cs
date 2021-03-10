using Arco.Core;
using Arco.Models;
using Arco.Services;
using Arco.Views;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace Arco
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        private readonly MainWindowViewModel _viewModel;
        //DispatcherTimer messageTimer = null;
        DispatcherTimer tokenTimer = null;
        DispatcherTimer historyDownloadTimer = null;

        HttpServer server = null;
        public MainWindow()
        {
            InitializeComponent();
            _viewModel = new MainWindowViewModel(DialogCoordinator.Instance);
            DataContext = _viewModel;

        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            Navigate_To_View((sender as MenuItem).Header.ToString());
        }


        public void Navigate_To_View(string headerName)
        {
            if (!(DataContext as MainWindowViewModel).LoggedIn && 
                !headerName.Equals("更新License信息") && 
                !headerName.Equals("参数设置") &&
                !headerName.Equals("登录"))
            {
                return;
            }

            _viewModel.CurrentUserControl = null;
            switch (headerName)
            {
                case "首页":
                    _viewModel.CurrentUserControl = new Home();
                    break;
                case "联系人":
                    _viewModel.CurrentUserControl = new Contacts();
                    break;
                case "常用联系人":
                    _viewModel.CurrentUserControl = new FavoriteContacts();
                    break;
                case "呼叫记录":
                    _viewModel.CurrentUserControl = new History();
                    break;
                case "操作记录":
                    _viewModel.CurrentUserControl = new Operations();
                    break;
                case "单点呼叫":
                    _viewModel.CurrentUserControl = new SingleCall();
                    break;
                case "话务监控":
                    _viewModel.CurrentUserControl = new Views.Monitor();
                    break;
                case "广播组":
                    _viewModel.CurrentUserControl = new Groups();
                    break;
                case "报警联动呼叫":
                    _viewModel.CurrentUserControl = new AlarmCall();
                    break;
                case "点名呼叫":
                    _viewModel.CurrentUserControl = new Views.RollCall();
                    break;
                case "预案管理":
                    _viewModel.CurrentUserControl = new UserPlans();
                    break;
                case "会议管理":
                    _viewModel.CurrentUserControl = new UserMeetings();
                    break;
                case "会议通话记录":
                    _viewModel.CurrentUserControl = new HistoryMeeting();
                    break;
                case "事件流管理":
                    _viewModel.CurrentUserControl = new Incidents();
                    break;
                case "录音文件收藏":
                    _viewModel.CurrentUserControl = new HistoryCollection();
                    break;
                case "用户管理":
                    _viewModel.CurrentUserControl = new Users();
                    break;
                case "部门管理":
                    _viewModel.CurrentUserControl = new Departments();
                    break;
                case "岗位管理":
                    _viewModel.CurrentUserControl = new ViewPositions();
                    break;
                case "电台设置":
                    _viewModel.CurrentUserControl = new Home();
                    break;
                case "本机信息":
                    _viewModel.CurrentUserControl = new Views.SystemConfig();
                    break;
                case "关于":
                    _viewModel.CurrentUserControl = new About();
                    break;
                case "更新License信息":
                    _viewModel.CurrentUserControl = new License();
                    break;
                case "参数设置":
                    _viewModel.CurrentUserControl = new LoginConfig();
                    break;
                case "登录":
                    _viewModel.CurrentUserControl = new Login();
                    break;
                case "退出":
                    Application.Current.Shutdown();
                    break;
            }
            if (_viewModel.CurrentUserControl != null)
            {
                DockPanel dockPanel = this.FindName("ContentPanel") as DockPanel;
                dockPanel.Children.Clear();
                _viewModel.CurrentUserControl.DataContext = this.DataContext;
                dockPanel.Children.Add(_viewModel.CurrentUserControl);
                _viewModel.Title = _viewModel.CurrentUserControl.ToString() + " shows.";
                DataService.GetInstance().Log("-", "主界面", "主界面", "跳转", headerName);
            }
        }

        private CustomDialog CustomDialoDiaPanel { get; set; }
        private async void Dial_Panel(object sender, RoutedEventArgs e)
        {
            MainWindow mainWindow = Application.Current.MainWindow as MainWindow;

            if (CustomDialoDiaPanel == null)
            {
                CustomDialoDiaPanel = new CustomDialog(mainWindow.MetroDialogOptions)
                {
                    Content = this.Resources["CustomDialoDiaPanel"],
                    DialogContentMargin = new GridLength(5),
                    Width = 190,
                    Height = 385,
                    DialogTitleFontSize = 16,
                    BorderThickness = new Thickness(2),
                    BorderBrush = Brushes.DarkGray,
                    DialogTop = this.Resources["DialogCloseDiaPanel"]
                };
                Grid topGrid = CustomDialoDiaPanel.DialogTop as Grid;
                topGrid.FindChild<Label>().SetCurrentValue(ContentProperty, "拨号");
                (topGrid.Children[1] as Button).Click += Dial_PanelClose;
            }

            await mainWindow.ShowMetroDialogAsync(CustomDialoDiaPanel);
        }
        private async void Dial_PanelClose(object sender, RoutedEventArgs e)
        {
            await ((MainWindow)Application.Current.MainWindow).HideMetroDialogAsync(CustomDialoDiaPanel);
        }
        private void Dial_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            if (_viewModel.DialNo == null)
            {
                _viewModel.DialNo = "";
            }
            _viewModel.DialNo += button.Content;
            _viewModel.NotifyChange("DialNo");
        }
        private CustomDialog CustomDialogCustomDialoTransfer { get; set; }
        private async void Transfer_Panel(object sender, RoutedEventArgs e)
        {
            if (_viewModel.ExtNoCall == null)
            {
                return;
            }

            MainWindow mainWindow = Application.Current.MainWindow as MainWindow;

            if (CustomDialogCustomDialoTransfer == null)
            {
                CustomDialogCustomDialoTransfer = new CustomDialog(mainWindow.MetroDialogOptions)
                {
                    Content = this.Resources["CustomDialoTransfer"],
                    DialogContentMargin = new GridLength(5),
                    Width = 600,
                    Height = 450,
                    DialogTitleFontSize = 16,
                    BorderThickness = new Thickness(2),
                    BorderBrush = Brushes.DarkGray,
                    DialogTop = this.Resources["CustomDialoTransferClose"]
                };
                Grid topGrid = CustomDialogCustomDialoTransfer.DialogTop as Grid;
                topGrid.FindChild<Label>().SetCurrentValue(ContentProperty, "转接");
                (topGrid.Children[1] as Button).Click += Transfer_Panel_Close;
            }

            await mainWindow.ShowMetroDialogAsync(CustomDialogCustomDialoTransfer);


        }
        private async void Transfer_Panel_Close(object sender, RoutedEventArgs e)
        {
            await ((MainWindow)Application.Current.MainWindow).HideMetroDialogAsync(CustomDialogCustomDialoTransfer);
        }
        private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is DataGrid)
            {
                object selectedItem = (sender as DataGrid).SelectedItem;
                if (selectedItem != null)
                {
                    if (selectedItem is Contact)
                    {
                        _viewModel.DialNo = (selectedItem as Contact).Devices[0].Number;
                    }
                }
            }
        }

        
        public async Task<bool> StartServices()
        {
            if (server != null) return true;
            server = new HttpServer(Convert.ToInt32(_viewModel.User.UserConfig.Localport), _viewModel);
            new Thread(new ThreadStart(server.Listen)).Start();

            await Task.Run(() => {
                Thread.Sleep(500);
            });
            if (!_viewModel.TempThreadResult)
            {
                return false;
            }

            tokenTimer = new DispatcherTimer();
            tokenTimer.Interval = TimeSpan.FromMinutes(1);
            tokenTimer.Tick += Timer_Tick_Token;
            tokenTimer.Start();
            
            //messageTimer = new DispatcherTimer();
            //DataService.GetInstance().ClearQueue();
            //messageTimer.Interval = TimeSpan.FromSeconds(1);
            //messageTimer.Tick += Timer_Tick_Message;
            //messageTimer.Start();
            //DataService.GetInstance().Log("NA", "主界面", "主界面", "消息队列", "消息队列检查定时器启动");

            historyDownloadTimer = new DispatcherTimer();
            historyDownloadTimer.Interval = TimeSpan.FromSeconds(5);
            historyDownloadTimer.Tick += Timer_Tick_historyDownloadTimer;
            historyDownloadTimer.Start();
            SetupCallBarStatus("空闲");

            return true;
        }

        private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private async void Timer_Tick_historyDownloadTimer(object sender, EventArgs e)
        {
            try
            {
                await DataService.GetInstance().DownloadCDRAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
            }
        }
        

        private async void Timer_Tick_Token(object sender, EventArgs e)
        {
            string tokenJson = "\"ipaddr\": \"" + _viewModel.User.UserConfig.Localip  + "\",port: \"" + _viewModel.User.UserConfig.Localport + "\"";
            await DataService.GetInstance().PostAsync(tokenJson, "/heartbeat", true);
        }
        private void ArcoWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_viewModel.LoggedIn)
            {
                try
                {
                    server?.Stop();
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex.StackTrace);
                }
                try
                {
                    tokenTimer?.Stop();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.StackTrace);
                }
                try
                {
                    historyDownloadTimer?.Stop();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.StackTrace);
                }           
            }
        }

        public void SetupCallBarStatus(string status)
        {
            switch (status)
            {
                case "空闲":
                    this.Dial_Button.SetCurrentValue(IsEnabledProperty, true);
                    this.Transfer_Button.SetCurrentValue(IsEnabledProperty, false);
                    this.Hold_Button.SetCurrentValue(IsEnabledProperty, false);
                    this.Retrive_Button.SetCurrentValue(IsEnabledProperty, false);
                    this.Meeting_Button.SetCurrentValue(IsEnabledProperty, false);
                    this.Busy_Button.SetCurrentValue(IsEnabledProperty, true);
                    this.Ready_Button.SetCurrentValue(IsEnabledProperty, false);
                    this.Hangup_Button.SetCurrentValue(IsEnabledProperty, false);
                    break;

                case "通话中":
                    this.Dial_Button.SetCurrentValue(IsEnabledProperty, false);
                    this.Transfer_Button.SetCurrentValue(IsEnabledProperty, true);
                    this.Hold_Button.SetCurrentValue(IsEnabledProperty, true);
                    this.Retrive_Button.SetCurrentValue(IsEnabledProperty, false);
                    this.Meeting_Button.SetCurrentValue(IsEnabledProperty, true);
                    this.Busy_Button.SetCurrentValue(IsEnabledProperty, false);
                    this.Ready_Button.SetCurrentValue(IsEnabledProperty, false);
                    this.Hangup_Button.SetCurrentValue(IsEnabledProperty, true);
                    break;

                case "保持":
                    this.Dial_Button.SetCurrentValue(IsEnabledProperty, false);
                    this.Transfer_Button.SetCurrentValue(IsEnabledProperty, false);
                    this.Hold_Button.SetCurrentValue(IsEnabledProperty, false);
                    this.Retrive_Button.SetCurrentValue(IsEnabledProperty, true);
                    this.Meeting_Button.SetCurrentValue(IsEnabledProperty, false);
                    this.Busy_Button.SetCurrentValue(IsEnabledProperty, false);
                    this.Ready_Button.SetCurrentValue(IsEnabledProperty, false);
                    this.Hangup_Button.SetCurrentValue(IsEnabledProperty, false);
                    break;

                case "示忙":
                    this.Dial_Button.SetCurrentValue(IsEnabledProperty, true);
                    this.Transfer_Button.SetCurrentValue(IsEnabledProperty, false);
                    this.Hold_Button.SetCurrentValue(IsEnabledProperty, false);
                    this.Retrive_Button.SetCurrentValue(IsEnabledProperty, false);
                    this.Meeting_Button.SetCurrentValue(IsEnabledProperty, false);
                    this.Busy_Button.SetCurrentValue(IsEnabledProperty, false);
                    this.Ready_Button.SetCurrentValue(IsEnabledProperty, true);
                    this.Hangup_Button.SetCurrentValue(IsEnabledProperty, false);
                    break;
            }

        }

        private async void Contact_Query(object sender, RoutedEventArgs e)
        {
            await Task.Run(() =>
            {
                _viewModel.Contacts = new ObservableCollection<Contact>();
                List<Contact> _list = new List<Contact>(_viewModel.BaseContacts).FindAll(
                (x) =>
                {
                    if (!_viewModel.IsOnlyFavorite)
                    {
                        return _viewModel.ContactSearchText == null || (x.Name.IndexOf(_viewModel.ContactSearchText) != -1 ||
                                x.Department.Name.IndexOf(_viewModel.ContactSearchText) != -1 ||
                                x.Position.Name.IndexOf(_viewModel.ContactSearchText) != -1
                                );
                    }
                    else
                    {
                        return x.IsFavorite &&
                                (_viewModel.ContactSearchText == null || (x.Name.IndexOf(_viewModel.ContactSearchText) != -1 ||
                                x.Department.Name.IndexOf(_viewModel.ContactSearchText) != -1 ||
                                x.Position.Name.IndexOf(_viewModel.ContactSearchText) != -1
                                ));
                    }
                }
                );
                _viewModel.Contacts = new ObservableCollection<Contact>(_list);
                _viewModel.NotifyChange("Contacts");
            });

        }

        private async void Dial_Click_Submit(object sender, RoutedEventArgs e)
        {
            if (_viewModel.DialNo == null || _viewModel.DialNo.Length == 0) return;
            string uri = "";
            string json = "";
            if (Convert.ToInt32(_viewModel.DialNo) >= Models.SystemConfig.Instance.ExtFrom &&
                    Convert.ToInt32(_viewModel.DialNo) <= Models.SystemConfig.Instance.ExtTo)
            {
                uri = "/extension/dial_extension";
                json = "{" +
                    "\"caller\": \"" + _viewModel.User.UserConfig.Extno1 + "\"," +
                    "\"callee\": \"" + _viewModel.DialNo + "\"," +
                    "\"autoanswer\": \"no\"" +
                    "}";
            }
            else
            {
                uri = "/extension/dial_outbound";
                json = "{" +
                    "\"extid\": \"" + _viewModel.User.UserConfig.Extno1 + "\"," +
                    "\"outto\": \"" + _viewModel.DialNo + "\"," +
                    "\"autoanswer\": \"no\"" +
                    "}";
            }


            _viewModel.Message = "正在呼叫" + _viewModel.DialNo + "...";
            await ((MainWindow)Application.Current.MainWindow).HideMetroDialogAsync(CustomDialoDiaPanel);
            JObject response = await DataService.GetInstance().PostAsync(json, uri, true);

            if (response != null && ((string)response["status"]).Equals("Success"))
            {
                _viewModel.Message += "成功";

                _viewModel.NotifyChange("Message");
                DataService.GetInstance().Log(_viewModel.User.Username, "主界面", "主界面", "呼叫", "呼叫" + _viewModel.DialNo + "成功");
            }
        }
        private CustomDialog CustomDialoContactMessage { get; set; }

        private async void Message_ContactAsyncClose(object sender, RoutedEventArgs e)
        {
            await ((MainWindow)Application.Current.MainWindow).HideMetroDialogAsync(CustomDialoContactMessage);
        }

        private async void Message_ContactAsync(object sender, RoutedEventArgs e)
        {
            MainWindow mainWindow = Application.Current.MainWindow as MainWindow;

            if (CustomDialoContactMessage == null)
            {
                CustomDialoContactMessage = new CustomDialog(mainWindow.MetroDialogOptions)
                {
                    Content = this.Resources["CustomDialoContactMessage"],
                    DialogContentMargin = new GridLength(5),
                    Width = 600,
                    Height = 400,
                    DialogTitleFontSize = 16,
                    BorderThickness = new Thickness(2),
                    BorderBrush = Brushes.DarkGray,
                    DialogTop = this.Resources["DialogClose"]
                };
                Grid topGrid = CustomDialoContactMessage.DialogTop as Grid;
                topGrid.FindChild<Label>().SetCurrentValue(ContentProperty, "短信");
                (topGrid.Children[1] as Button).Click += Message_ContactAsyncClose;
            }

            await mainWindow.ShowMetroDialogAsync(CustomDialoContactMessage);
        }

        private async void Hangup_Button_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.ExtNoCall != null)
            {
                string json = "{ \"extid\": \"" + _viewModel.User.UserConfig.Extno1 + "\" }";
                _viewModel.Message = "正在挂断通话：" + _viewModel.ExtNoCall.Caller + "->" + _viewModel.ExtNoCall.Callee + "...";

                JObject response = await DataService.GetInstance().PostAsync(json, "/extension/hangup", true);

                if (response != null && ((string)response["status"]).Equals("Success"))
                {
                    _viewModel.Message += "成功";
                    this.SetupCallBarStatus("空闲");
                    _viewModel.NotifyChange("Message");
                    DataService.GetInstance().Log(_viewModel.User.Username, "主界面", "主界面", "呼叫", "挂断" + _viewModel.DialNo + "成功");
                }
            }

        }

        private async void Busy_Button_Click(object sender, RoutedEventArgs e)
        {
            string json = "{ \"queueid\": \"" + _viewModel.User.UserConfig.Queue + "\", \"extid\": \"" + _viewModel.User.UserConfig.Extno1 + "\" }";
            _viewModel.Message = "正在示忙" + _viewModel.User.UserConfig.Extno1 + "...";

            JObject response = await DataService.GetInstance().PostAsync(json, "/queue/pause_agent", true);

            if (response != null && ((string)response["status"]).Equals("Success"))
            {
                _viewModel.Message += "成功";
                this.SetupCallBarStatus("示忙");
                _viewModel.NotifyChange("Message");
                DataService.GetInstance().Log(_viewModel.User.Username, "主界面", "主界面", "呼叫", "示忙" + _viewModel.User.UserConfig.Extno1 + "成功");
            }
        }

        private async void Ready_Button_Click(object sender, RoutedEventArgs e)
        {
            string json = "{ \"queueid\": \"" + _viewModel.User.UserConfig.Queue + "\", \"extid\": \"" + _viewModel.User.UserConfig.Extno1 + "\" }";
            _viewModel.Message = "正在示闲" + _viewModel.User.UserConfig.Extno1 + "...";

            JObject response = await DataService.GetInstance().PostAsync(json, "/queue/unpause_agent", true);

            if (response != null && ((string)response["status"]).Equals("Success"))
            {
                _viewModel.Message += "成功";
                this.SetupCallBarStatus("空闲");
                _viewModel.NotifyChange("Message");
                DataService.GetInstance().Log(_viewModel.User.Username, "主界面", "主界面", "呼叫", "示闲" + _viewModel.User.UserConfig.Extno1 + "成功");
            }
        }

        private async void Check_Queue_Button_Click(object sender, RoutedEventArgs e)
        {
            string json = "{\"queueid\": \"" + _viewModel.User.UserConfig.Queue + "\"}";
            JObject response = await DataService.GetInstance().PostAsync(json, "/queuestatus/query", true);

            if (response != null && ((string)response["status"]).Equals("Success"))
            {
                JArray queues = (JArray)response["queues"];
                JArray queuestatus = (JArray)queues[0]["queuestatus"];
                _viewModel.Message = "当前排队人数：" + (string)queuestatus[0]["callercount"];
            }
        }


        private async void Transfer_Button_Call_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.ExtNoCall != null)
            {
                string json = "";
                string url = "";
                //outbound
                if (_viewModel.ExtNoCall.Caller.Equals(_viewModel.User.UserConfig.Extno1)
                    && Convert.ToInt32(_viewModel.ExtNoCall.Callee) >= Models.SystemConfig.Instance.ExtFrom &&
                    Convert.ToInt32(_viewModel.ExtNoCall.Callee) <= Models.SystemConfig.Instance.ExtTo)
                {
                    json = "{\"outboundid\": \"" + _viewModel.ExtNoCall.CalleeChannelId +
                            "\",\"number\": \"" + _viewModel.User.UserConfig.Extno1 +
                            "\", \"fromext\":\"" + _viewModel.DialNo + "\"}";
                    url = "/inbound/transfer_number";

                }
                else
                {
                    //inbound
                    if (_viewModel.ExtNoCall.Callee.Equals(_viewModel.User.UserConfig.Extno1)
                        && Convert.ToInt32(_viewModel.ExtNoCall.Caller) >= Models.SystemConfig.Instance.ExtFrom &&
                    Convert.ToInt32(_viewModel.ExtNoCall.Caller) <= Models.SystemConfig.Instance.ExtTo)
                    {
                        json = "{\"inboundid\": \"" + _viewModel.ExtNoCall.CallerChannelId +
                            "\",\"number\": \"" + _viewModel.User.UserConfig.Extno1 +
                            "\", \"fromext\":\"" + _viewModel.DialNo + "\"}";
                        url = "/inbound/transfer_number";

                    }
                    //ext to ext
                    else
                    {
                        json = "{\"callid\": \""
                            + _viewModel.ExtNoCall.CallId + "\",\"transferor\": \""
                            + _viewModel.User.UserConfig.Extno1 + "\", \"transferto\":\"" +
                            _viewModel.DialNo + "\", \"fromext\":\""
                            + _viewModel.User.UserConfig.Extno1 + "\"}";
                        url = "/calltransfer";
                    }
                }


                _viewModel.Message = "正在转接通话：" + _viewModel.ExtNoCall.Caller + "->" + _viewModel.ExtNoCall.Callee + "...";

                JObject response = await DataService.GetInstance().PostAsync(json, url, true);

                if (response != null && ((string)response["status"]).Equals("Success"))
                {
                    _viewModel.Message += "成功";
                    this.SetupCallBarStatus("空闲");
                    _viewModel.NotifyChange("Message");
                    DataService.GetInstance().Log(_viewModel.User.Username, "主界面", "主界面", "呼叫", "转接" + _viewModel.DialNo + "成功");
                }
            }

            await ((MainWindow)Application.Current.MainWindow).HideMetroDialogAsync(CustomDialogCustomDialoTransfer);
        }

        private async void Hold_Button_Click(object sender, RoutedEventArgs e)
        {

            if (_viewModel.ExtNoCall != null)
            {
                string json = "{\"extid\": \"" + _viewModel.User.UserConfig.Extno1 + "\"}";
                _viewModel.Message = "正在保持通话：" + _viewModel.ExtNoCall.Caller + "->" + _viewModel.ExtNoCall.Callee + "...";

                JObject response = await DataService.GetInstance().PostAsync(json, "/extension/hold", true);

                if (response != null && ((string)response["status"]).Equals("Success"))
                {
                    _viewModel.Message += "成功";
                    this.SetupCallBarStatus("保持");
                    _viewModel.NotifyChange("Message");
                    DataService.GetInstance().Log(_viewModel.User.Username, "主界面", "主界面", "呼叫", "保持" + _viewModel.ExtNoCall.Caller + "成功");
                }
            }
        }

        private async void Retrive_Button_Click(object sender, RoutedEventArgs e)
        {

            if (_viewModel.ExtNoCall != null)
            {
                string json = "{\"extid\": \"" + _viewModel.User.UserConfig.Extno1 + "\"}";
                _viewModel.Message = "正在取回通话：" + _viewModel.ExtNoCall.Caller + "->" + _viewModel.ExtNoCall.Callee + "...";

                JObject response = await DataService.GetInstance().PostAsync(json, "/extension/unhold", true);

                if (response != null && ((string)response["status"]).Equals("Success"))
                {
                    _viewModel.Message += "成功";
                    this.SetupCallBarStatus("通话中");
                    _viewModel.NotifyChange("Message");
                    DataService.GetInstance().Log(_viewModel.User.Username, "主界面", "主界面", "呼叫", "取回" + _viewModel.ExtNoCall.Caller + "成功");
                }
            }
        }


        private CustomDialog CustomDialogMeeting { get; set; }
        private async void CustomDialogMeeting_Close(object sender, RoutedEventArgs e)
        {
            await ((MainWindow)Application.Current.MainWindow).HideMetroDialogAsync(CustomDialogMeeting);
        }
        private async void Meeting_Button_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.ExtNoCall == null)
            {
                return;
            }
            MainWindow mainWindow = Application.Current.MainWindow as MainWindow;

            if (CustomDialogMeeting == null)
            {
                CustomDialogMeeting = new CustomDialog(mainWindow.MetroDialogOptions)
                {
                    Content = this.Resources["CustomDialogMeeting"],
                    DialogContentMargin = new GridLength(5),
                    Width = 600,
                    Height = 450,
                    DialogTitleFontSize = 16,
                    BorderThickness = new Thickness(2),
                    BorderBrush = Brushes.DarkGray,
                    DialogTop = this.Resources["CustomDialogMeetingClose"]
                };
                Grid topGrid = CustomDialogMeeting.DialogTop as Grid;
                topGrid.FindChild<Label>().SetCurrentValue(ContentProperty, "会议");
                (topGrid.Children[1] as Button).Click += CustomDialogMeeting_Close;
            }

            await mainWindow.ShowMetroDialogAsync(CustomDialogMeeting);
        }

        private async void Meeting_Button_Call_Click(object sender, RoutedEventArgs e)
        {

            if (_viewModel.ExtNoCall != null)
            {
                _viewModel.Message = "会议邀请：" + _viewModel.DialNo + "...";
                string json = "{" + "\"bargein\": \"" + _viewModel.DialNo + "\"," + "\"bargedext\": \"" + _viewModel.User.UserConfig.Extno1 + "\"" + "}";
                JObject response = await DataService.GetInstance().PostAsync(json, "/extension/barge", true);
                if (response != null)
                {
                    if (((string)response["status"]).Equals("Success"))
                    {
                        _viewModel.Message += "成功";
                        DataService.GetInstance().Log(_viewModel.User.Username, "主界面", "主界面", "呼叫", "会议" + _viewModel.DialNo + "成功");
                    }
                    else
                    {
                        _viewModel.Message += "失败";
                    }
                }
                else
                {
                    _viewModel.Message += "失败";
                }
            }
            await ((MainWindow)Application.Current.MainWindow).HideMetroDialogAsync(CustomDialogMeeting);
        }
    }
}

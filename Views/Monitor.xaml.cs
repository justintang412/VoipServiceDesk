using Arco.Core;
using Arco.Models;
using Arco.Services;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
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
    /// Monitor.xaml 的交互逻辑
    /// </summary>
    public partial class Monitor : UserControl
    {
        private MainWindowViewModel _viewModel = null;
        public Monitor()
        {
            InitializeComponent();
        }
        public Meeting ChosenMeeting { get; set; }
        public Call ChosenCall { get; set; }
        public string ContactSearchText { get; set; }
        public bool IsOnlyFavorite { get; set; }

        public ObservableCollection<Contact> MeetingContacts { get; set; }

        private void Control_Loaded(object sender, RoutedEventArgs e)
        {
            _viewModel = (DataContext as MainWindowViewModel);
            _viewModel.Monitor = this;
            _viewModel.ProcessAtControl = this.ProcessAtControl;
            _viewModel.NotifyChange("Monitor");
        }
        private void ProcessAtControl(params object[] values)
        {
            JObject result = values[0] as JObject;
            string action = (string)result["action"];

            if (action.Equals("ANSWER") || action.Equals("ANSWERED"))
            {
                Cdr_Grid.Items.Refresh();
            }
            
        }
        private void Control_Unloaded(object sender, RoutedEventArgs e)
        {
            _viewModel.ProcessAtControl = null;
            _viewModel.Monitor = null;
        }

        private async void Disconnnect_Button_Click(object sender, RoutedEventArgs e)
        {
            if (Cdr_Grid.SelectedItem == null) return;
            _viewModel.ChosenCall = Cdr_Grid.SelectedItem as Call;
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
                    Convert.ToInt32(_viewModel.ChosenCall.Callee) <= Models.SystemConfig.Instance.ExtTo)
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
                }
                else
                {
                    _viewModel.Message += "失败";
                }
            }
        }

        private async void Listen_Button_Click(object sender, RoutedEventArgs e)
        {
            if (Cdr_Grid.SelectedItem == null) return;
            _viewModel.ChosenCall = Cdr_Grid.SelectedItem as Call;
            if (_viewModel.ExtNoCall != null && _viewModel.ChosenCall.CallId.Equals(_viewModel.ExtNoCall.CallId))
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
                }
                else
                {
                    _viewModel.Message += "失败";
                }
            }
        }

        private async void Insert_Button_Click(object sender, RoutedEventArgs e)
        {
            if (Cdr_Grid.SelectedItem == null) return;
            _viewModel.ChosenCall = Cdr_Grid.SelectedItem as Call;
            if (_viewModel.ChosenCall.CallId.Equals(_viewModel.ExtNoCall.CallId))
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
                }
                else
                {
                    _viewModel.Message += "失败";
                }
            }
        }

        private void Selection_Changed(object sender, SelectionChangedEventArgs e)
        {
            Call call = (Call)(sender as DataGrid)?.SelectedItem;
            if (call != null)
            {
                ChosenCall = call;
                
            }
        }


    }
}

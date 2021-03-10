using Arco.Models;
using Arco.Services;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Arco.Views
{
    /// <summary>
    /// SingleCall.xaml 的交互逻辑
    /// </summary>
    public partial class SingleCall : UserControl
    {
        private MainWindowViewModel _viewModel = null;
        public SingleCall()
        {
            InitializeComponent();
            Contacts = new ObservableCollection<Contact>();
            TotalPages = 0;
            Page = 1;
            IsOnlyFavorite = false;
        }
        public Contact CurrentContact { get; set; }
        public bool IsOnlyFavorite { get; set; }
        public ObservableCollection<Contact> Contacts { get; set; }
        private void Control_Loaded(object sender, RoutedEventArgs e)
        {
            _viewModel = (DataContext as MainWindowViewModel);
            _viewModel.SingleCall = this;
            _viewModel.ProcessAtControl = ProcessAtControl;
            LoadData();
        }

        private void ProcessAtControl(params object[] values)
        {
            JObject result = values[0] as JObject;
            DeviceList.Items.Refresh();
            Contacts_Grid.Items.Refresh();
        }
        private async Task LoadData()
        {
            Contacts = await Task.Run(() =>
            {
                List<Contact> _list = new List<Contact>(_viewModel.BaseContacts).FindAll(
                (x) =>
                {
                    if (!IsOnlyFavorite)
                    {
                        return SearchText == null || (x.Name.Contains(SearchText) ||
                                x.Department.Name.Contains(SearchText) ||
                                x.Position.Name.Contains(SearchText) ||
                                x.Devices[0].Number.Contains(SearchText) ||
                                x.Devices[1].Number.Contains(SearchText)
                                );
                    }
                    else
                    {
                        return x.IsFavorite && (SearchText == null || (x.Name.Contains(SearchText) ||
                                x.Department.Name.Contains(SearchText) ||
                                x.Position.Name.Contains(SearchText) ||
                                x.Devices[0].Number.Contains(SearchText) ||
                                x.Devices[1].Number.Contains(SearchText)
                                ));
                    }
                }
                );
                return new ObservableCollection<Contact>(_list);
            });

            long totalCount = Contacts.Count;
            if (totalCount % 500 > 0)
            {
                TotalPages = Convert.ToInt32(totalCount / 500 + 1);
            }
            else
            {
                TotalPages = Convert.ToInt32(totalCount / 500);
            }
            _viewModel.NotifyChange("SingleCall");
        }
        private void Pre_Page(object sender, RoutedEventArgs e)
        {
            if (Page > 1)
            {
                Page = Page - 1;
            }
            LoadData();
        }

        public int Page { get; set; }
        public int TotalPages { get; set; }
        public string SearchText { get; set; }

        private void Next_Page(object sender, RoutedEventArgs e)
        {
            if (Page < TotalPages)
            {
                Page = Page + 1;
            }
            LoadData();

        }

        private void Go_Page(object sender, RoutedEventArgs e)
        {
            if (Page <= TotalPages && Page >= 1)
            {
                LoadData();
            }
        }

        private void Search_Button(object sender, RoutedEventArgs e)
        {
            LoadData();
        }

        private void Contacts_Grid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CurrentContact = (Contact)(sender as DataGrid).SelectedItem;

            _viewModel.NotifyChange("SingleCall");
        }

        private void DeviceList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _viewModel.DialNo = ((Device)(sender as DataGrid).SelectedItem)?.Number;
        }

        private void Download_Click(object sender, RoutedEventArgs e)
        {

        }

        private async void Call_All(object sender, RoutedEventArgs e)
        {
            if (CurrentContact == null)
            {
                return;
            }
            for (int i = 0; i < CurrentContact.Devices.Count; i++)
            {
                string uri = "";
                string json = "";
                if (CurrentContact.Devices[i].Number.Length>0 && Convert.ToInt32(CurrentContact.Devices[i].Number) >= Models.SystemConfig.Instance.ExtFrom && 
                    Convert.ToInt32(CurrentContact.Devices[i].Number) <= Models.SystemConfig.Instance.ExtTo)
                {
                    uri = "/extension/dial_extension";
                    json = "{" +
                        "\"caller\": \"" + _viewModel.User.UserConfig.Extno1 + "\"," +
                        "\"callee\": \"" + CurrentContact.Devices[i].Number + "\"," +
                        "\"autoanswer\": \"no\"" +
                        "}";
                }
                else
                {
                    uri = "/extension/dial_outbound";
                    json = "{" +
                        "\"extid\": \"" + _viewModel.User.UserConfig.Extno1 + "\"," +
                        "\"outto\": \"" + CurrentContact.Devices[i].Number + "\"," +
                        "\"autoanswer\": \"no\"" +
                        "}";
                }

                _viewModel.Message = "正在呼叫" + CurrentContact.Devices[i].Number + "...";
                JObject response = await DataService.GetInstance().PostAsync(json, uri, true);

                if (response != null && ((string)response["status"]).Equals("Success"))
                {
                    string callid = (string)response["callid"];
                    _viewModel.Message += "成功";

                    Contact _contact = CurrentContact;
                    await Task.Run(new Action(() =>
                    {
                        for (int second = 0; second < Models.SystemConfig.Instance.Calltimeout; second++)
                        {
                            Thread.Sleep(1000);

                            if (_contact.Status != null && _contact.Status.Equals("通话中"))
                            {
                                break;
                            }
                        }
                    }));

                    if (_contact.Status != null && _contact.Status.Equals("通话中"))
                    {
                        break;
                    }
                    else
                    {
                        Call _call = new List<Call>(_viewModel.Calls).Find(c => c.CallId.Equals(callid));
                        if (_call != null)
                        {
                            _viewModel.Message = "正在挂断通话：" + _call.Caller + "->" + _call.Callee + "...";

                            JObject hangupResponse = await DataService.GetInstance().PostAsync("{ \"extid\": \"" + _viewModel.User.UserConfig.Extno1 + "\" }", "/extension/hangup", true);

                            if (hangupResponse != null && ((string)hangupResponse["status"]).Equals("Success"))
                            {
                                _viewModel.Message += "成功";
                                ((MainWindow)Application.Current.MainWindow).SetupCallBarStatus("空闲");
                                _viewModel.NotifyChange("Message");
                            }
                        }
                    }

                }
                CurrentContact.Devices[i].Status = "未接通";
                DeviceList.Items.Refresh();
            }
            Call_All_Button.SetCurrentValue(IsEnabledProperty, true);
        }

        private void Control_Unloaded(object sender, RoutedEventArgs e)
        {
            _viewModel.ProcessAtControl = null;
            _viewModel.SingleCall = null;
        }
    }
}

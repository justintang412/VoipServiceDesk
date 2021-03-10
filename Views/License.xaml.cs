using Arco.Core;
using Arco.Services;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Management;
using System.Windows;
using System.Windows.Controls;

namespace Arco.Views
{
    /// <summary>
    /// License.xaml 的交互逻辑
    /// </summary>
    public partial class License : UserControl
    {
        private MainWindowViewModel _viewModel = null;
       
        public License()
        {
            InitializeComponent();
        }

        private void Save(object sender, RoutedEventArgs e)
        {
            UpdateDefinition<BsonDocument> update = Builders<BsonDocument>.Update
                .Set("license",LicenseTextBox.Text);
            DataService.GetInstance().Database.GetCollection<BsonDocument>("systemconfig")
                .UpdateOne(Builders<BsonDocument>.Filter.Empty, update);
            MessageTextBox.SetCurrentValue(TextBlock.TextProperty, "License已经更新，请重新启动应用程序。");
            
        }

        private void Control_Loaded(object sender, RoutedEventArgs e)
        {
            _viewModel = (DataContext as MainWindowViewModel);
            _viewModel.License = this;


            string sysinfo = "";
            string cpuInfo = "";
            ManagementObjectCollection mocWin32_Processor = (new ManagementClass("Win32_Processor")).GetInstances();
            foreach (ManagementObject mo in mocWin32_Processor)
            {
                cpuInfo += mo.Properties["ProcessorId"].Value.ToString();
            }
            sysinfo += cpuInfo + "/";

            string HDid = "";
            ManagementClass mc = new ManagementClass("Win32_DiskDrive");
            ManagementObjectCollection moc = mc.GetInstances();
            foreach (ManagementObject mo in moc)
            {
                HDid += (string)mo.Properties["Model"].Value;
            }
            sysinfo += HDid;
            SIDCodeTextBox.SetCurrentValue(TextBox.TextProperty, sysinfo.ToMD5());
        }

        private void Control_Unloaded(object sender, RoutedEventArgs e)
        {
            _viewModel.ProcessAtControl = null;
            _viewModel.License = null;
        }
    }
}

using Arco.Core;
using Arco.Models;
using Arco.Services;
using MongoDB.Bson;
using MongoDB.Driver;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Security;
using System;
using System.Linq;
using System.Management;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Arco.Views
{
    /// <summary>
    /// LoginConfig.xaml 的交互逻辑
    /// </summary>
    public partial class LoginConfig : UserControl
    {
        private MainWindowViewModel _viewModel = null;
        public LoginConfig()
        {
            InitializeComponent();
        }
        public User MyUser { get; set; }
        private void Control_Loaded(object sender, RoutedEventArgs e)
        {
            _viewModel = (DataContext as MainWindowViewModel);
            _viewModel.LoginConfig = this;


            FilterDefinition<BsonDocument> filter = Builders<BsonDocument>.Filter.Eq("user", _viewModel.TempUserName);

            MyUser = DataService.GetInstance().Database.GetCollection<BsonDocument>("sysuser")
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

                        }).FirstOrDefault();
            _viewModel.NotifyChange("LoginConfig");
        }

        private void Control_Unloaded(object sender, RoutedEventArgs e)
        {
            _viewModel.ProcessAtControl = null;
            _viewModel.LoginConfig = null;
        }

        private async void Save(object sender, RoutedEventArgs e)
        {
            if (MyUser.UserConfig.Localip == null ||
                MyUser.UserConfig.Localport == null ||
                MyUser.UserConfig.Serverip == null ||
                MyUser.UserConfig.Serverport == null ||
                MyUser.UserConfig.Pbxuser == null ||
                MyUser.UserConfig.Extno1 == null ||
                MyUser.UserConfig.Extno2 == null ||
                MyUser.UserConfig.Queue == null
                ) return;

            bool isLicensed = false;

            await Task.Run(() =>
            {
                try
                {
                    string publickey = "<RSAKeyValue><Modulus>wr3saKb/4L/fY85P+aOVWMxDXebfxzLXlDOweIT4l1iPxWdx8becvh9H0vxD2xLDjlxJYlJ+8rdrZwlQck0Dd5PYvqp5aFOkKU3eiZFM3bfkzi0lTKH7pKmtDc2bv1ZNTa5/eyzh5+QiLJdYBDt8FzjSkIq98mNCR6Zv1SvAMdE=</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>";
                    RSACryptoServiceProvider publicRsa = new RSACryptoServiceProvider();
                    publicRsa.FromXmlString(publickey);
                    RSAParameters rp = publicRsa.ExportParameters(false);
                    AsymmetricKeyParameter pbk = DotNetUtilities.GetRsaPublicKey(rp);
                    IBufferedCipher c = CipherUtilities.GetCipher("RSA/ECB/PKCS1Padding");
                    c.Init(false, pbk);

                    byte[] DataToDecrypt = Convert.FromBase64String(Models.SystemConfig.Instance.License);
                    byte[] outBytes = c.DoFinal(DataToDecrypt);//解密
                    string strDec = Encoding.UTF8.GetString(outBytes);
                    string[] licenseinfo = strDec.Split(',');
                    if (licenseinfo != null && licenseinfo.Length == 2)
                    {
                        string sid = licenseinfo[0];
                        string endtime = licenseinfo[1];

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
                        if (sysinfo.ToMD5().Equals(sid) && endtime.CompareTo(DateTime.Now.ToString("yyyy-MM-dd")) > 0)
                        {
                            isLicensed = true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    isLicensed = false;
                    Console.WriteLine(ex.StackTrace);
                }
            });

            if (!isLicensed)
            {
                _viewModel.LoggedIn = false;
                _viewModel.NavigateToView.Execute("更新License信息");
                return;
            }

            FilterDefinition<BsonDocument> filter = Builders<BsonDocument>.Filter.Eq("user", MyUser.Username);
            UpdateDefinition<BsonDocument> update = Builders<BsonDocument>.Update
                .Set("isadmin", MyUser.Isadmin ? "true" : "false")
                .Set("config.localip", MyUser.UserConfig.Localip)
                .Set("config.localport", MyUser.UserConfig.Localport)
                .Set("config.serverip", MyUser.UserConfig.Serverip)
                .Set("config.serverport", MyUser.UserConfig.Serverport)
                .Set("config.pbxuser", MyUser.UserConfig.Pbxuser)
                .Set("config.extno1", MyUser.UserConfig.Extno1)
                .Set("config.extno2", MyUser.UserConfig.Extno2)
                .Set("config.queue", MyUser.UserConfig.Queue);
            if (MyPasswordBox.Password.Length > 0)
            {
                update = update.Set("pass", MyPasswordBox.Password.ToMD5());
            }
            if (PBXPasswordBox.Password.Length > 0)
            {
                update = update.Set("config.pbxpass", PBXPasswordBox.Password.ToMD5());
            }
            if (PBXQueuePasswordBox.Password.Length > 0)
            {
                update = update.Set("config.queuepass", PBXQueuePasswordBox.Password);
            }
            DataService.GetInstance().Database.GetCollection<BsonDocument>("sysuser").UpdateOne(filter, update);
            _viewModel.LoggedIn = false;
            _viewModel.NavigateToView.Execute("登录");
        }
    }
}

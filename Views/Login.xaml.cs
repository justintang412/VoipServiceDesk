using Arco.Core;
using Arco.Models;
using Arco.Services;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json.Linq;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Security;
using System;
using System.Management;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Arco.Views
{
    /// <summary>
    /// Login.xaml 的交互逻辑
    /// </summary>
    public partial class Login : UserControl
    {
        MainWindowViewModel _viewModel;
        public bool SubmitClicked { get; set; }
        public bool SubmitEnabled { get; set; }
        public bool IsLoginError { get; set; }
        public string Username { get; set; }
        public Login()
        {
            InitializeComponent();
            fillBackground();


        }
        private void fillBackground()
        {
            ImageBrush b = new ImageBrush();
            b.ImageSource = new BitmapImage(new Uri("pack://application:,,,/Assets/stars.png"));
            b.Stretch = Stretch.Fill;
            this.Background = b;
        }
        private async void Submit_Click(object sender, RoutedEventArgs e)
        {
            SubmitClicked = true;
            SubmitEnabled = false;
            _viewModel.TempUserName = Username;
            _viewModel.NotifyChange("Login");

            bool isDatabaseReady = false;

            await Task.Run(() =>
            {
                try
                {
                    MongoClientSettings mongoClientSettings = MongoClientSettings.FromUrl(new MongoUrl("mongodb://localhost:27017"));
                    mongoClientSettings.ServerSelectionTimeout = new TimeSpan(0, 0, 3);
                    MongoClient mongoClient = new MongoClient(mongoClientSettings);
                    IMongoDatabase database = mongoClient.GetDatabase("arco");
                    database.ListCollections();
                    isDatabaseReady = true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.StackTrace);
                }
            });

            
            if (!isDatabaseReady)
            {
                SubmitClicked = false;
                SubmitEnabled = true;
                IsLoginError = true;
                _viewModel.LoggedIn = false;
                _viewModel.Message = "系统未正确安装，请重新安装";
                _viewModel.NotifyChange("Login");
                return;
            }

            if (Models.SystemConfig.Instance.License == null)
            {
                SubmitClicked = false;
                SubmitEnabled = true;
                IsLoginError = true;
                _viewModel.LoggedIn = false;
                _viewModel.Message = "系统未正确安装，请重新安装";
                _viewModel.NotifyChange("Login");
                return;
            }

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
                SubmitClicked = false;
                SubmitEnabled = true;
                IsLoginError = true;
                _viewModel.LoggedIn = false;
                _viewModel.NavigateToView.Execute("更新License信息");
                return;
            }


            IFindFluent<BsonDocument, BsonDocument> mongoCollection = DataService.GetInstance().Database.GetCollection<BsonDocument>("sysuser")
                .Find(Builders<BsonDocument>.Filter.Eq("user", Username));
            if (mongoCollection.CountDocuments() == 0)
            {
                SubmitClicked = false;
                SubmitEnabled = true;
                IsLoginError = true;
                _viewModel.Message = "登录失败，请检查是否正确输入用户名和密码";
                _viewModel.NotifyChange("Login");
                return;
            }
            BsonDocument userinfoDocument = mongoCollection.First();
            if (userinfoDocument != null && PasswordBox.Password.ToMD5().Equals(((string)userinfoDocument["pass"])))
            {
                _viewModel.User = new User
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
                DataService.GetInstance().Ip = _viewModel.User.UserConfig.Serverip;
                DataService.GetInstance().Port = _viewModel.User.UserConfig.Serverport;
                DataService.GetInstance().Version = _viewModel.Version;

                string json =
                    "{"
                    + "\"username\": \"" + _viewModel.User.UserConfig.Pbxuser + "\","
                    + "\"password\": \"" + _viewModel.User.UserConfig.Pbxpass + "\","
                    + "\"port\": \"" + _viewModel.User.UserConfig.Localport + "\","
                    + "\"version\": \"1.0.2\","
                    + "\"url\": \"" + _viewModel.User.UserConfig.Localip + ":" + _viewModel.User.UserConfig.Localport + "\","
                    + "\"urltag\": \"1\""
                    + "}";
                Console.WriteLine(json);
                JObject response = await DataService.GetInstance().PostAsync(json, "/login", false);

                if (response != null && ((string)response["status"]).Equals("Success"))
                {
                    DataService.GetInstance().Token = (string)response["token"];

                    MainWindow mainWindow = Application.Current.MainWindow as MainWindow;
                    if (await mainWindow.StartServices())
                    {
                        
                        _viewModel.UpdateData();
                        _viewModel.LoggedIn = true;
                        _viewModel.Message = "";
                        _viewModel.NotifyChange("Login");
                        _viewModel.NavigateToView.Execute("首页");
                        DataService.GetInstance().Log(_viewModel.User.Username, "登录", "登录", "登录", "登录成功");
                        return;
                    }
                    else
                    {
                        SubmitClicked = false;
                        SubmitEnabled = true;
                        IsLoginError = true;
                        _viewModel.LoggedIn = false;
                        
                        _viewModel.NavigateToView.Execute("参数设置");
                        return;
                    }
                    
                }
                else
                {
                    SubmitClicked = false;
                    SubmitEnabled = true;
                    IsLoginError = true;
                    _viewModel.LoggedIn = false;
                    _viewModel.NavigateToView.Execute("参数设置");
                    return;
                }
            }
            else
            {
                SubmitClicked = false;
                SubmitEnabled = true;
                IsLoginError = true;
                _viewModel.Message = "登录失败，请检查是否正确输入用户名和密码";
                _viewModel.NotifyChange("Login");
                return;
            }
        }


        private void Login_loaded(object sender, RoutedEventArgs e)
        {
            _viewModel = this.DataContext as MainWindowViewModel;
            SubmitClicked = false;
            SubmitEnabled = true;
            IsLoginError = false;
            _viewModel.Login = this;

            
            UserNameTextBox.Focus();
            _viewModel.NotifyChange("Login");

            
        }

        private void Control_Unloaded(object sender, RoutedEventArgs e)
        {
            _viewModel.ProcessAtControl = null;
            _viewModel.Login = null;
        }

        private async void Reinstall_Click(object sender, RoutedEventArgs e)
        {
            SubmitClicked = true;
            SubmitEnabled = false;
            

            bool isInstall = false;

            await Task.Run(() =>
            {
                try
                {
                    
                }
                catch (Exception ex)
                {
                    
                }
            });

            if (!isInstall)
            {
                SubmitClicked = false;
                SubmitEnabled = true;
                IsLoginError = true;
                _viewModel.LoggedIn = false;
                _viewModel.NavigateToView.Execute("更新License信息");
                return;
            }
        }
    }
}

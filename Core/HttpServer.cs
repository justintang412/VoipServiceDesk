using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Windows;

namespace Arco.Core
{
    public class HttpServer
    {
        protected int port;
        TcpListener listener;
        bool is_active = true;
        MainWindowViewModel _viewModel = null;
        Thread thread = null;
        public HttpServer(int port, MainWindowViewModel model)
        {
            this.port = port;
            this._viewModel = model;
        }

        public void Listen()
        {
            _viewModel.TempThreadResult = true;
            try
            {
                listener = new TcpListener(IPAddress.Parse(_viewModel.User.UserConfig.Localip), port);
                listener.Start();
                while (is_active)
                {
                    TcpClient s = listener.AcceptTcpClient();
                    EventProcessor processor = new EventProcessor(s, this);
                    thread = new Thread(new ThreadStart(processor.process));
                    thread.Start();
                    Thread.Sleep(1);
                }
                listener.Stop();
            }
            catch(Exception ex)
            {
                _viewModel.TempThreadResult = false;
                Console.WriteLine(ex.StackTrace);
            }
            
            


        }
        public void Stop()
        {
            is_active = false;
            try
            {
                listener.Stop();
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
            }
            try
            {
                thread?.Abort();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
            }

        }
        public void HandleGETRequest(EventProcessor p)
        {
        }
        public void HandlePOSTRequest(EventProcessor p, StreamReader inputData)
        {
            string data = inputData.ReadToEnd();
            try
            {
                ProcessEventAsync(data);
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
            }

        }
        private void ProcessEventAsync(string jsondata)
        {
            JObject result = JsonConvert.DeserializeObject<JObject>(jsondata);
            _viewModel.CurrentUserControl?.Dispatcher.BeginInvoke(new Action(() =>
            {
                _viewModel.PreprocessHttpEvent(result);
            }));
        }
    }
}
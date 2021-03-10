using Arco.Core;
using Arco.Models;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Arco.Services
{
    public class CallService
    {
        MainWindowViewModel _model = null;
        
        public CallService(MainWindowViewModel model)
        {
            _model = model;
        }
    }
}

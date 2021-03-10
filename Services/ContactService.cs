using Arco.Core;
using Arco.Models;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Arco.Services
{
    public class ContactService
    {
        public List<Department> Departments { set; get; }
        public List<DeviceType> DeviceTypes { set; get; }
        public List<Call> Call { 
            get { 
                return DataService.GetInstance().Database.GetCollection<Call>("Call").Find(new BsonDocument { }).ToList(); 
            } 
        }
        public List<Contact> Contacts
        {
            get
            {
                return DataService.GetInstance().Database.GetCollection<Contact>("Contact").Find(new BsonDocument { }).ToList();
            }
        }

        public ObservableCollection<Message> Messages { get; set; }
        public ContactService()
        {
            if (Messages == null)
            {
                Messages = new ObservableCollection<Message>();
            }
            else
            {
                Messages.Clear();
            }
            Messages.Concat(DataService.GetInstance().Database.GetCollection<Message>("Message").Find(new BsonDocument { }).ToList());
        }

    }
}

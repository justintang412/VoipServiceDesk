using System.Collections.ObjectModel;

namespace Arco.Models
{
    public class Group
    {
        public string Groupid { get; set; }
        public string Name { set; get; }
        public string Number { get; set; }
        public string Duplex { set; get; }
        public ObservableCollection<Contact> AllowExten { get; set; }
        public string EnableKeyHanup { get; set; }
        public string MulticastIp { get; set; }
       
    }
}

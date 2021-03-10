using System.Collections.ObjectModel;

namespace Arco.Models.Org
{
    public class OrgPosition
    {
        public Position Position { get; set; }
        public ObservableCollection<Contact> Contacts { get; set; }
    }
}
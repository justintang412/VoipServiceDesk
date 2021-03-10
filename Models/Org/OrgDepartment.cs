using System.Collections.ObjectModel;

namespace Arco.Models.Org
{
    public class OrgDepartment
    {
        public Department Department { get; set; }
        public ObservableCollection<OrgDepartment> Childen { get; set; }
        public ObservableCollection<Contact> Contacts { get; set; }
    }
}
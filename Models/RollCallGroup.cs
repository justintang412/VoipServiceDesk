using System.Collections.ObjectModel;

namespace Arco.Models
{
    public class RollCallGroup
    {
        public string Id { get; set; }
        public string Name { set; get; }
        public string Code { get; set; }
        public string Remark { set; get; }
        public string Rank { get; set; }
        public ObservableCollection<ContactWithSingleDevice> Members { set; get; }
    }
}

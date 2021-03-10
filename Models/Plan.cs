using System.Collections.ObjectModel;

namespace Arco.Models
{
    public class Plan
    {
        public string Name { get; set; }
        public string Number { get; set; }
        public string Content { get; set; }
        public ObservableCollection<ContactWithSingleDevice> Members { get; set; }

        public bool IsInCall { get; set; }
    }
}

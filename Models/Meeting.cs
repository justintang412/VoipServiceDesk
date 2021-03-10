using System.Collections.ObjectModel;

namespace Arco.Models
{
    public class Meeting
    {
        public string Name { get; set; }
        public string Number { get; set; }
        public string Content { get; set; }
        public string Starttime { get; set; }
        public ObservableCollection<ContactWithSingleDevice> Members { get; set; }

        public bool IsInCall { get; set; }

    }
}

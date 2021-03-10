using System.Collections.ObjectModel;

namespace Arco.Models
{
    public class Incident
    {
        public string Id { get; set; }
        public string Type { get; set; }
        public string ReportTime { get; set; }
        public string ReportNumber { get; set; }
        public string ReportContent { get; set; }
        public ObservableCollection<CallHistory> CallHistories { get; set; }
    }
}

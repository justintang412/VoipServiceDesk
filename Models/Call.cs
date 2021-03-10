namespace Arco.Models
{
    public class Call
    {
        //need to change this to adapt call munipulation
        public string CallId { get; set; }
        public string Caller { get; set; }
        public ContactWithSingleDevice CallerContact { get; set; }
        public string CallerChannelId { get; set; }
        public string Callee { get; set; }
        public ContactWithSingleDevice CalleeContact { get; set; }
        public string CalleeChannelId { get; set; }

        public string Direction { get; set; }
        public string Start { get; set; }
        public string CallTimeSeconds { get; set; }
        public string RingTimeSeconds { get; set; }
        public string TalkTimeSeconds { get; set; }
        public string Recording { get; set; }
    }
}

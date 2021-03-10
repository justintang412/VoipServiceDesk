using MongoDB.Bson;
using System;

namespace Arco.Models
{
    public class Message
    {
        public ObjectId Id { set; get; }
        public string From { set; get; }
        public string To { get; set; }
        public string MessageTime { get; set; }
        public string Content { get; set; }
        public string Status { set; get; }

    }
}

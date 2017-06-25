using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Scheduler.Models.Custom
{
    public class Notification
    {
        [BsonId]
        [BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
        public string Id { get; set; }
        public string Title { get; set; }
        public string Body { get; set; }
        public string UserEmail { get; set; }    
        public DateTime SendingTime { get; set; }
        public bool Seen { get; set; }
        public string Type { get; set; }
    }
}
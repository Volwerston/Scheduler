using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Scheduler.Models
{
    public class SingleTask
    {
        public string Title { get; set; }
        public string Description { get; set; }
        [BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
        public string TargetId { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public DateTime Date { get; set; }
        public string Status { get; set; }
    }
}
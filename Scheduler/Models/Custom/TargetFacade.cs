using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Scheduler.Models
{
    public class TargetFacade
    {
        [BsonId]
        [BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
        public string Id { get; set; }
        public string Title { get; set; }
        public List<string> Tags { get; set; }
        [BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
        public string TargetId { get; set; }
        public DateTime StartDate { get; set; }
        public int Difficulty { get; set; }
        public string UserEmail { get; set; }
    }
}
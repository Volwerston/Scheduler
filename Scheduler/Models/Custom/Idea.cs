using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Scheduler.Models
{
    public class Idea
    {
        [BsonId]
        [BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
        public string TargetId { get; set; }

        public string Text { get; set; }
    }
}
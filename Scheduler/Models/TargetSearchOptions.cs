using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace Scheduler.Models
{
    public class TargetSearchOptions
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string LastObjectId { get; set; }
        public string Title { get; set; }
        public string OrderBy { get; set; }
        public List<string> Tags { get; set; }
        public string UserName { get; set; }
    }
}
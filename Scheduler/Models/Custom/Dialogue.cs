using MongoDB.Bson.Serialization.Attributes;
using Scheduler.Models.Auxiliary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Scheduler.Models.Custom
{
    public class Dialogue
    {
        [BsonId]
        [BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
        public string Id { get; set; }
        public DateTime CreationTime { get; set; }
        public MessageWriter Writer1 { get; set; }
        public MessageWriter Writer2 { get; set; }
    }
}
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Scheduler.Models.Custom
{
    public class TaskDone
    {
        [BsonId]
        [BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
        public string Id { get; set; }
        public string TaskName { get; set; }
        [BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
        public string TaskId { get; set; }
        public string NextTaskName { get; set; }
        [BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
        public string NextTaskId { get; set; }
        public string UserEmail { get; set; }
        public DateTime FinishDate { get; set; }
        public bool IsPublic { get; set; }
        public string Solution { get; set; }
    }
}
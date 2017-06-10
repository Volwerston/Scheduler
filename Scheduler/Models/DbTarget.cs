using Algorithms;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Scheduler.Models
{
    public class DbTarget
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
        public string Name { get; set; }
        public int Duration { get; set; }
        public int Elapsed { get; set; }
        public int WeekendsRemained { get; set; }
        public int Difficulty { get; set; }
        public int DailyDuration { get; set; }
        public WorkSpan BestWorkSpan { get; set; }
        public List<DayOfWeek> WorkingDays { get; set; }
        public List<int> PreTargets { get; set; }
        public List<string> Solution { get; set; }
        public string UserEmail { get; set; }
        public int ActiveDays { get; set; }
        public DbTarget NextTarget { get; set; }
        public DateTime StartDate { get; set; }
    }
}
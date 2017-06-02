using MongoDB.Bson;
using System;
using System.Collections.Generic;

namespace Algorithms
{
    public class Target
    {
        public int Id { get; set; }
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
        public Target NextTarget { get; set; }
        public DateTime StartDate { get; set; }
    }
}

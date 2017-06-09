using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace Scheduler.Models
{
    public class TargetSearchOptions
    {
        public ObjectId LastObjectId { get; set; }
        public string Title { get; set; }
        public string OrderBy { get; set; }
        public List<string> Tags { get; set; }
        public string UserName { get; set; }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Scheduler.Models
{
    public class TaskStatusPostParams
    {
        public string Status { get; set; }
        public int TaskNumber { get; set; }
        public DateTime Date { get; set; }
    }
}
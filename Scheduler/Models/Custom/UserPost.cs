using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Scheduler.Models.Custom
{
    public class UserPost
    {
        public int Id { get; set; }
        public string Text { get; set; }
        public string UserEmail { get; set; }
        public DateTime AddingTime { get; set; }
    }
}
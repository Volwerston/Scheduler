using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Scheduler.Models.Auxiliary
{
    public class FindUserPostsParams
    {
        public int LastId { get; set; }
        public string Email { get; set; }
    }
}
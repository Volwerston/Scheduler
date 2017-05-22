using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Scheduler.Models.Auxiliary
{
    public class StartPageBlockData
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string ColorName { get; set; }
        public string NextBlock { get; set; }
        public string ArrowImageSrc { get; set; }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Scheduler.Models
{
    public class IdeasChunkParams
    {
        public string TargetId { get; set; }
        public int ChunkNumber { get; set;}
        public int EntitiesNum { get; set; }
    }
}
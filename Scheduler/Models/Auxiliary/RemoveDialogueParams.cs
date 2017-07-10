using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Scheduler.Models.Auxiliary
{
    public class RemoveDialogueParams
    {
        public string DialogueId { get; set; }
        public DateTime LastDeletionTime { get; set; }
        public string UserEmail { get; set; }
    }
}
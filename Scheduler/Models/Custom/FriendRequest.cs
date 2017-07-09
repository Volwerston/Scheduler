using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Scheduler.Models.Custom
{
    public class FriendRequest
    {
        public int Id { get; set; }
        public string SenderMail { get; set; }
        public string RecipientMail { get; set; }
        public DateTime SendingTime { get; set; }
    }
}
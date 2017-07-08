using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Scheduler.Models.Auxiliary
{
    public class UnformedScheduleMailService : MailService
    {
        public override string GetMailBody(object context)
        {
            return "We could not form your schedule for today. Maybe you are too busy:) So please visit your personal account to make manual settings for you schedule!";
        }
    }
}
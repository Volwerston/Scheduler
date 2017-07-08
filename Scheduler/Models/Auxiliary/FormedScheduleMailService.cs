using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace Scheduler.Models.Auxiliary
{
    public class FormedScheduleMailService : MailService
    {
        public override string GetMailBody(object context)
        {
            List<DbTarget> targets = context as List<DbTarget>;

            if(targets == null)
            {
                throw new Exception("Email context invalid");
            }

            StringBuilder toReturn = new StringBuilder();

            toReturn.Append("Here is your default schedule for today. Feel free to change it on your account page!<br/><br/>");

            foreach(var target in targets)
            {
                toReturn.Append("<div style=\"min-width: 300px; min-height: 150px; word-break: break-all; border: 1px solid black\">");
                toReturn.Append("<p style=\"text-align: center;\">" + target.Name + "</p>");
                toReturn.Append("<p><b>Start: </b>" + target.BestWorkSpan.StartTime.ToString() + "</p>");
                toReturn.Append("<p><b>Finish: </b>" + target.BestWorkSpan.EndTime.ToString() + "</p>");
                toReturn.Append("</div>");
                toReturn.Append("<br/>");
            }

            return toReturn.ToString();
        }
    }
}
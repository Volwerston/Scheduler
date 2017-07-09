using Scheduler.Models.Custom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Scheduler.Models.Auxiliary
{
    public abstract class NotificationHandler
    {
        public NotificationHandler Next { get; set; }

        public void Accept(Notification n, string status)
        {
            if(CanHandle(n))
            {
                Handle(n, status);
            }
            else
            {
                if(Next != null)
                {
                    Next.Accept(n, status);
                }
            }
        }

        public abstract bool CanHandle(Notification n);

        public abstract void Handle(Notification n, string status);
    }
}
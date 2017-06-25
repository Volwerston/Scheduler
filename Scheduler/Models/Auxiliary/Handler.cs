using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Scheduler.Models.Auxiliary
{
    public abstract class Handler
    {
        public Handler Next { get; set; }

        public abstract bool CanHandle(SingleTask t);

        public void Accept(SingleTask t, List<Tuple<DbTarget, DbTarget>> targets, string userEmail)
        {
            if(CanHandle(t))
            {
                Handle(t, targets, userEmail);
            }
            else
            {
                if(Next != null)
                {
                    Next.Accept(t, targets, userEmail);
                }
            }
        }

        public abstract void Handle(SingleTask t, List<Tuple<DbTarget, DbTarget>> targets, string userEmail);
    }
}
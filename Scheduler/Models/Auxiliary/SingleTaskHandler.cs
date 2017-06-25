using MongoDB.Driver;
using Scheduler.Models.Custom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Scheduler.Models.Auxiliary
{
    public class SingleTaskHandler : Handler
    {
        public override bool CanHandle(SingleTask t)
        {
            return String.IsNullOrEmpty(t.TargetId) && String.IsNullOrEmpty(t.Status);
        }

        public override void Handle(SingleTask t, List<Tuple<DbTarget, DbTarget>> targets, string userEmail)
        {
            Notification toAdd = new Notification()
            {
                Body = t.Description,
                Title = t.Title,
                SendingTime = DateTime.Now,
                UserEmail = userEmail,
                Type = "SingleTask",
                Seen = false
            };

            MongoClient client = new MongoClient();
            var db = client.GetDatabase("scheduler");
            var collection = db.GetCollection<Notification>("notifications");

            collection.InsertOne(toAdd);
        }
    }
}
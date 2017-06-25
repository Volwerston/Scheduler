using MongoDB.Driver;
using Scheduler.Models.Custom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Scheduler.Models.Auxiliary
{
    public class OmmitedTargetTaskHandler : Handler
    {
        public override bool CanHandle(SingleTask t)
        {
            return !String.IsNullOrEmpty(t.TargetId) && String.IsNullOrEmpty(t.Status);
        }

        public override void Handle(SingleTask t, List<Tuple<DbTarget, DbTarget>> targets, string userEmail)
        {
            Notification toAdd = new Notification()
            {
                Body = t.Description,
                Title = t.Title,
                SendingTime = DateTime.Now,
                UserEmail = userEmail,
                Type = "TargetTask",
                Seen = false
            };

            MongoClient client = new MongoClient();
            var db = client.GetDatabase("scheduler");
            var collection = db.GetCollection<Notification>("notifications");
            var targetCollection = db.GetCollection<DbTarget>("targets");

            collection.InsertOne(toAdd);

            Tuple<DbTarget, DbTarget> tuple = targets.Where(x => x.Item2.Id == t.TargetId).Single();

            if (tuple.Item2.WeekendsRemained == 0)
            {
                tuple.Item2.Elapsed = tuple.Item2.Elapsed + 1;
            }
            else
            {
                tuple.Item2.WeekendsRemained = tuple.Item2.WeekendsRemained - 1;
            }

            var filter = Builders<DbTarget>.Filter.Where(x => x.Id == tuple.Item1.Id);

            targetCollection.ReplaceOne(filter, tuple.Item1);

            targets.Remove(tuple);
        }
    }
}
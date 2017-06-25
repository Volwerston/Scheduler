using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Scheduler.Models.Auxiliary
{
    public class FailedTargetTaskHandler : Handler
    {
        public override bool CanHandle(SingleTask t)
        {
            return !String.IsNullOrEmpty(t.TargetId) && t.Status == "Failed";
        }

        public override void Handle(SingleTask t, List<Tuple<DbTarget, DbTarget>> targets, string userEmail)
        {
            MongoClient client = new MongoClient();
            var db = client.GetDatabase("Scheduler");
            var collection = db.GetCollection<DbTarget>("targets");

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

            collection.ReplaceOne(filter, tuple.Item1);

            targets.Remove(tuple);
        }
    }
}
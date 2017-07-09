using MongoDB.Bson;
using MongoDB.Driver;
using Scheduler.Models.Custom;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Web;

namespace Scheduler.Models.Auxiliary
{
    public class TargetTaskNotificationHandler : NotificationHandler
    {
        public override bool CanHandle(Notification n)
        {
            return !String.IsNullOrEmpty(n.Type) && n.Type == "TargetTask";
        }

        public override void Handle(Notification n, string status)
        {
            MongoClient client = new MongoClient();
            var db = client.GetDatabase("scheduler");
            var targets = db.GetCollection<DbTarget>("targets");

            if(status == "Done")
            {

                var bufTargets = targets.Find(Builders<DbTarget>.Filter.Where(x => x.UserEmail == n.UserEmail));
                var userTargets = bufTargets.ToList();
                List<Tuple<DbTarget, DbTarget>> currTargets = new List<Tuple<DbTarget, DbTarget>>();

                foreach (var target in userTargets)
                {
                    DbTarget curr = target;

                    while (curr != null)
                    {
                        if (curr.StartDate != default(DateTime) && curr.ActiveDays < curr.Duration && curr.WorkingDays.Contains(n.SendingTime.AddDays(-1.0).DayOfWeek))
                        {
                            currTargets.Add(new Tuple<DbTarget, DbTarget>(target, curr));
                            break;
                        }

                        if (curr.StartDate == default(DateTime)) break;

                        curr = curr.NextTarget;
                    }
                }

                Tuple<DbTarget, DbTarget> tuple = currTargets.Where(x => x.Item2.Name == n.Title).SingleOrDefault();

                if(tuple != default(Tuple<DbTarget, DbTarget>))
                {
                    tuple.Item2.ActiveDays = tuple.Item2.ActiveDays + 1;

                    if (tuple.Item2.ActiveDays == tuple.Item2.Duration)
                    {
                        if (tuple.Item2.NextTarget == null)
                        {
                            HandleFinishedFinalTask(tuple, n.UserEmail, db);
                        }
                        else
                        {
                            HandleFinishedTask(tuple, n.UserEmail, db);
                        }
                    }

                    targets.FindOneAndReplace(x => x.Id == tuple.Item1.Id, tuple.Item1);
                }
            }
        }

        public void HandleFinishedTask(Tuple<DbTarget, DbTarget> tuple, string userEmail, IMongoDatabase db)
        {
            tuple.Item2.NextTarget.StartDate = DateTime.Now;
            tuple.Item2.NextTarget.Id = ObjectId.GenerateNewId().ToString();

            TaskDone td = new TaskDone()
            {
                FinishDate = DateTime.Now.AddDays(-1.0),
                TaskId = tuple.Item2.Id,
                NextTaskId = tuple.Item2.NextTarget.Id,
                TaskName = tuple.Item2.Name,
                NextTaskName = tuple.Item2.NextTarget.Name,
                UserEmail = userEmail,
                Solution = tuple.Item2.Solution,
                IsPublic = tuple.Item2.IsPublic
            };

            IMongoCollection<TaskDone> col = db.GetCollection<TaskDone>("tasksDone");

            col.InsertOne(td);

            int bonus = 100 * Convert.ToInt32(Math.Ceiling(Convert.ToDouble(tuple.Item2.ActiveDays / (tuple.Item2.Elapsed - (tuple.Item2.Difficulty - tuple.Item2.WeekendsRemained)))));

            Notification n = new Notification()
            {
                Body = String.Format("You finished the task: {0}. Our congratulations! Your next task: {1}. Wish you success! By the way, we give you {2} bonuses for using Premium account services", tuple.Item2.Name, tuple.Item2.NextTarget.Name, bonus),
                Title = "Task finished",
                Seen = false,
                SendingTime = DateTime.Now,
                Type = "TaskDone",
                UserEmail = userEmail
            };

            AddBonusForUser(userEmail, bonus);

            IMongoCollection<Notification> col2 = db.GetCollection<Notification>("notifications");
            col2.InsertOne(n);
        }

        public void HandleFinishedFinalTask(Tuple<DbTarget, DbTarget> tuple, string userEmail, IMongoDatabase db)
        {
            TaskDone td = new TaskDone()
            {
                FinishDate = DateTime.Now.AddDays(-1.0),
                TaskId = tuple.Item2.Id,
                NextTaskId = null,
                TaskName = tuple.Item2.Name,
                NextTaskName = null,
                UserEmail = userEmail,
                Solution = tuple.Item2.Solution,
                IsPublic = tuple.Item2.IsPublic
            };

            IMongoCollection<TaskDone> col = db.GetCollection<TaskDone>("tasksDone");

            col.InsertOne(td);
            int bonus = 100 * Convert.ToInt32(Math.Ceiling(Convert.ToDouble(tuple.Item2.ActiveDays / (tuple.Item2.Elapsed - (tuple.Item2.Difficulty - tuple.Item2.WeekendsRemained)))));

            Notification n = new Notification()
            {
                Body = String.Format("You finished the task: {0}. Our congratulations! You finished the whole task successfully! By the way, we give you {1} bonuses for using Premium account services", tuple.Item2.Name, bonus),
                Title = "Task finished",
                Seen = false,
                SendingTime = DateTime.Now,
                Type = "FinalTaskDone",
                UserEmail = userEmail
            };

            AddBonusForUser(userEmail, bonus);

            IMongoCollection<Notification> col2 = db.GetCollection<Notification>("notifications");
            col2.InsertOne(n);
        }

        private void AddBonusForUser(string userEmail, int bonus)
        {
            using (SqlConnection con = new SqlConnection(System.Web.Configuration.WebConfigurationManager.ConnectionStrings["DBCS"].ConnectionString))
            {
                SqlCommand cmd = new SqlCommand("Update AspNetUsers Set Bonus = Bonus + @toAdd Where Email = @email", con);
                cmd.Parameters.AddWithValue("@toAdd", bonus);
                cmd.Parameters.AddWithValue("@email", userEmail);

                con.Open();
                cmd.ExecuteNonQuery();
            }
        }
    }
}
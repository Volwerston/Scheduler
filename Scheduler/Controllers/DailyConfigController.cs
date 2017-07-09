using MongoDB.Driver;
using Scheduler.Models;
using Scheduler.Models.Auxiliary;
using Scheduler.Models.Custom;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Hosting;
using System.Web.Http;
using System.Xml.Serialization;

namespace Scheduler.Controllers
{
    [RoutePrefix("api/DailyConfig")]
    public class DailyConfigController : ApiController
    {

        [NonAction]
        private bool IsDayProceeded(DateTime date)
        {
            try
            {
                List<DateTime> d = null;

                XmlSerializer ser = new XmlSerializer(typeof(List<DateTime>));
                using (FileStream s = new FileStream(HostingEnvironment.MapPath("~/Common/Text files/Journal.xml"), FileMode.Open, FileAccess.Read))
                {
                    d = (List<DateTime>)ser.Deserialize(s);
                }

                if (d == null) return false;
                return d.Select(x => x.Date).Contains(date.Date);
            }
            catch
            {
                return false;
            }
        }

        [NonAction]
        private void AddDateToJournal(DateTime toAdd)
        {
            try
            {
                List<DateTime> d = null;

                XmlSerializer ser = new XmlSerializer(typeof(List<DateTime>));
                using (FileStream s = new FileStream(HostingEnvironment.MapPath("~/Common/Text files/Journal.xml"), FileMode.Open, FileAccess.Read))
                {
                    d = (List<DateTime>)ser.Deserialize(s);
                }

                if (d == null)
                {
                    d = new List<DateTime>();
                }
                d.Add(toAdd);

                using (FileStream s = new FileStream(HostingEnvironment.MapPath("~/Common/Text files/Journal.xml"), FileMode.OpenOrCreate, FileAccess.Write))
                {
                    ser.Serialize(s, d);
                }
            }
            catch
            {
            }
        }

        [Route("ConfigureTasks")]
        public async Task<IHttpActionResult> Post()
        {

            if (IsDayProceeded(DateTime.Now.AddDays(-1.0)))
            {
                return Ok("Success");
            }

            try
            {

                int neededTimeOffset = 24 - DateTime.Now.Hour + TimeZone.CurrentTimeZone.GetUtcOffset(DateTime.Now).Hours;
                if (neededTimeOffset > 12) neededTimeOffset -= 12;

                List<string> emails = GetUserEmails(neededTimeOffset);
                List<Tuple<DbTarget, DbTarget>> toReturn = new List<Tuple<DbTarget, DbTarget>>();
                MongoClient client = new MongoClient();
                var db = client.GetDatabase("scheduler");
                IMongoCollection<DbTarget> targets = db.GetCollection<DbTarget>("targets");
                var notifications = db.GetCollection<Notification>("notifications");
                var scheduleCollection = db.GetCollection<Schedule>("schedules");

                foreach (var email in emails)
                {
                    List<Handler> handlers = new List<Handler>();
                    handlers.Add(new SingleTaskHandler());
                    handlers.Add(new DoneTargetTaskHandler());
                    handlers.Add(new FailedTargetTaskHandler());
                    handlers.Add(new OmmitedTargetTaskHandler());

                    for (int i = 1; i < handlers.Count(); ++i)
                    {
                        handlers[i - 1].Next = handlers[i];
                    }

                    var bufTargets = await targets.FindAsync(Builders<DbTarget>.Filter.Where(x => x.UserEmail == email));
                    var userTargets = bufTargets.ToList();

                    foreach (var target in userTargets)
                    {
                        DbTarget curr = target;

                        while (curr != null)
                        {
                            if (curr.StartDate != default(DateTime) && curr.ActiveDays < curr.Duration && curr.WorkingDays.Contains(Convert.ToDateTime(DateTime.Now.AddDays(-1.0)).DayOfWeek))
                            {
                                toReturn.Add(new Tuple<DbTarget, DbTarget>(target, curr));
                                break;
                            }

                            if (curr.StartDate == default(DateTime)) break;

                            curr = curr.NextTarget;
                        }
                    }

                    Schedule s = scheduleCollection.AsQueryable().ToList().Where(x => x.UserEmail == email && x.Date.Date == DateTime.Now.AddDays(-1.0).Date).SingleOrDefault();

                    foreach (var task in s.Tasks)
                    {
                        handlers[0].Accept(task, toReturn, email);
                    }


                    foreach (var target in toReturn)
                    {
                        Notification n = new Notification()
                        {
                            Body = "You didn't work on this target yesterday",
                            Title = target.Item2.Name,
                            UserEmail = email,
                            Seen = false,
                            Type = "OmmitedTarget",
                            SendingTime = DateTime.Now
                        };

                        notifications.InsertOne(n);

                        if (target.Item2.WeekendsRemained == 0)
                        {
                            target.Item2.Elapsed = target.Item2.Elapsed + 1;
                        }
                        else
                        {
                            target.Item2.WeekendsRemained = target.Item2.WeekendsRemained - 1;
                        }

                        targets.FindOneAndReplace(x => x.Id == target.Item1.Id, target.Item1);
                    }

                    List<Tuple<DbTarget, DbTarget>> nextTargets = Algorithms.Algorithms.GetCurrentTargets(DateTime.Now.DayOfWeek, email);
                    List<DbTarget> orderedTasks = Algorithms.Algorithms.DistributeDayTasks(nextTargets.Select(x => x.Item2).ToList());

                        if (orderedTasks != null)
                        {
                            MailService service = new FormedScheduleMailService();
                            string message = service.GetMailBody(orderedTasks);
                            service.SendEmail("btsemail1@gmail.com", email, message);
                        }
                        else
                        {
                            MailService service = new UnformedScheduleMailService();
                            string message = service.GetMailBody(null);
                            service.SendEmail("btsemail1@gmail.com", email, message);
                        }

                        Schedule schedule = new Schedule()
                        {
                            Date = DateTime.Now,
                            Tasks = orderedTasks.Select(x => new SingleTask()
                            {
                                Date = DateTime.Now,
                                Description = "",
                                StartTime = x.BestWorkSpan.StartTime,
                                EndTime = x.BestWorkSpan.EndTime,
                                Title = x.Name,
                                TargetId = x.Id,
                                Status = null
                            })
                             .ToList(),
                            UserEmail = email
                        };

                        scheduleCollection.InsertOne(schedule);               
                }

                AddDateToJournal(DateTime.Now.AddDays(-1.0));

                return Ok("Success");
            }
            catch (Exception e)
            {
                return InternalServerError(e);
            }
        }

        [NonAction]
        private List<string> GetUserEmails(int offset)
        {
            List<string> toReturn = new List<string>();

            using (SqlConnection connection = new SqlConnection(ConfigurationManager.ConnectionStrings["DBCS"].ConnectionString))
            {
                SqlCommand cmd = new SqlCommand("SELECT UserEmail from UserInfo Where TimeZone = @zone", connection);
                cmd.Parameters.AddWithValue("@zone", offset);

                connection.Open();

                SqlDataReader rdr = cmd.ExecuteReader();
                while (rdr.Read())
                {
                    toReturn.Add(rdr["Email"].ToString());
                }
            }

            return toReturn;
        }
    }
}

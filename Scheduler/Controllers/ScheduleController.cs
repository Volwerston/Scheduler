using MongoDB.Driver;
using Scheduler.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;

namespace Scheduler.Controllers
{
    [RoutePrefix("api/Schedule")]
    [Authorize]
    public class ScheduleController : ApiController
    {
        private static MongoClient client { get; set; }
        private static IMongoDatabase db { get; set; }
        private static IMongoCollection<Schedule> collection { get; set; }

        static ScheduleController()
        {
            client = new MongoClient();
            db = client.GetDatabase("scheduler");
            collection = db.GetCollection<Schedule>("schedules");
        }

        [Route("GetSchedule")]
        public IHttpActionResult Post([FromBody]DateTime userTime)
        {
            try
            {
                var query = collection.AsQueryable().ToList().Where(x => x.Date.Date == userTime.Date && x.UserEmail == User.Identity.Name);
                return Ok(query.SingleOrDefault());
            }
            catch (Exception e)
            {
                return InternalServerError(e);
            }
        }

        [Route("DeleteTask")]
        public async Task<IHttpActionResult> Delete([FromBody]DeleteTaskParams param)
        {
            try
            {
                Schedule s = collection.AsQueryable().ToList().Where(x => x.Date.Date == param.Date.Date && x.UserEmail == User.Identity.Name).Single();
                s.Tasks.RemoveAt(param.Num - 1);

                var filter = Builders<Schedule>.Filter.Where(x => x.Id == s.Id);
                var update = Builders<Schedule>.Update.Set(x => x.Tasks, s.Tasks);

                await collection.UpdateOneAsync(filter, update);

                return Ok("Success");
            }
            catch(Exception e)
            {
                return InternalServerError(e);
            }
        }

        [Route("GetPossibleTargets")]
        public async Task<IHttpActionResult> Put([FromBody]string userTime)
        {
            try
            {
                IMongoCollection<DbTarget> targets = db.GetCollection<DbTarget>("targets");
                var bufTargets = await targets.FindAsync(Builders<DbTarget>.Filter.Where(x => x.UserEmail == User.Identity.Name));
                List<DbTarget> toReturn = new List<DbTarget>();

                var userTargets = bufTargets.ToList();

                foreach (var target in userTargets)
                {
                    DbTarget curr = target;

                    while (curr != null)
                    {
                        if (curr.StartDate != default(DateTime) && curr.ActiveDays < curr.Duration && curr.WorkingDays.Contains(Convert.ToDateTime(userTime).DayOfWeek))
                        {
                            curr.NextTarget = null;
                            toReturn.Add(curr);
                            break;
                        }


                        curr = curr.NextTarget;
                    }
                }

                return Ok(toReturn);
            }
            catch (Exception e)
            {
                return InternalServerError(e);
            }
        }

        [Route("SetTaskStatus")]
        public async Task<IHttpActionResult> Post([FromBody]TaskStatusPostParams param)
        {
            try
            {
                Schedule s = collection.AsQueryable().ToList().Where(x => x.Date.Date == param.Date.Date && x.UserEmail == User.Identity.Name).Single();

                s.Tasks[param.TaskNumber - 1].Status = param.Status;

                var filter = Builders<Schedule>.Filter.Where(x => x.Id == s.Id);
                var update = Builders<Schedule>.Update.Set(x => x.Tasks, s.Tasks);

                await collection.UpdateOneAsync(filter, update);

                return Ok("Success");
            }
            catch(Exception e)
            {
                return InternalServerError(e);
            }
        }

        [Route("PostTask")]
        public async Task<IHttpActionResult> Post([FromBody]SingleTask task)
        {
            try
            {
                var query = collection.AsQueryable().ToList().Where(x => x.Date.Date == task.Date.Date && x.UserEmail == User.Identity.Name);
                Schedule s = query.FirstOrDefault();

                if (s == null)
                {
                    s = new Schedule()
                    {
                        Date = task.Date,
                        UserEmail = User.Identity.Name,
                        Tasks = new List<SingleTask>()
                    };

                    s.Tasks = s.Tasks.OrderBy(x => x.StartTime).ToList();
                    s.Tasks.Add(task);

                    await collection.InsertOneAsync(s);
                }
                else
                {
                    s.Tasks.Add(task);
                    s.Tasks = s.Tasks.OrderBy(x => x.StartTime).ToList();
                    var filter = Builders<Schedule>.Filter.Where(x => x.Id == s.Id);
                    var update = Builders<Schedule>.Update.Set(x => x.Tasks, s.Tasks);

                    await collection.UpdateOneAsync(filter, update);
                }

                return Ok("Success");
            }
            catch (Exception e)
            {
                return InternalServerError(e);
            }
        }

    }
}

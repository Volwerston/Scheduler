using FuzzyString;
using MongoDB.Driver;
using Scheduler.Models;
using Scheduler.Models.Custom;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;

namespace Scheduler.Controllers
{
    [Authorize]
    [RoutePrefix("api/Algorithms")]
    public class AlgorithmsController : ApiController
    {
        [Route("GetAlgorithms")]
        public async Task<IHttpActionResult> Get()
        {
            try
            {
                List<Algorithm> toReturn = new List<Algorithm>();

                MongoClient client = new MongoClient();
                var db = client.GetDatabase("scheduler");
                var collection = db.GetCollection<Algorithm>("algorithms");

                toReturn = await collection.FindAsync(Builders<Algorithm>.Filter.Empty).Result.ToListAsync();

                return Ok(toReturn);
            }
            catch(Exception e)
            {
                return InternalServerError(e);
            }
        }

        [Route("AddLogEntry")]
        public async Task<IHttpActionResult> Post([FromBody]AlgoLogEntry entry)
        {
            try
            {
                MongoClient client = new MongoClient();
                var db = client.GetDatabase("scheduler");
                var collection = db.GetCollection<AlgoLogEntry>("algorithmsLog");
                var algos = db.GetCollection<Algorithm>("algorithms");

                Algorithm a = algos.Find(Builders<Algorithm>.Filter.Where(x => x.Id == entry.AlgorithmId)).SingleOrDefault();
                if(a == null)
                {
                    throw new Exception("Algorithm not found");
                }

                if (CanBuyAlgorithm(User.Identity.Name, a.Bonuses))
                {
                    await collection.InsertOneAsync(entry);
                }

                return Ok("Success");
            }
            catch(Exception e)
            {
                return InternalServerError(e);
            }
        }

        [Route("WorkingTimeAnalysis")]
        public async Task<IHttpActionResult> Post()
        {
            List<Tuple<string,int[]>> toReturn = new List<Tuple<string,int[]>>();
            MongoClient client = new MongoClient();
            var db = client.GetDatabase("scheduler");
            var collection = db.GetCollection<Schedule>("schedules");

            List<Schedule> userData = await collection.FindAsync(Builders<Schedule>.Filter.Where(x => x.UserEmail == User.Identity.Name)).Result.ToListAsync();

            int[] done = new int[8640];
            int[] failed = new int[8640];
            int[] unknown = new int[8640];

            IEnumerable<SingleTask> doneTasks = userData.SelectMany(x => x.Tasks).Where(x => x.Status == "Done");
            IEnumerable<SingleTask>  failedTasks = userData.SelectMany(x => x.Tasks).Where(x => x.Status == "Failed");
            IEnumerable<SingleTask> unknownTasks = userData.SelectMany(x => x.Tasks).Where(x => String.IsNullOrEmpty(x.Status));

            ProcessTasks(doneTasks, done);
            ProcessTasks(failedTasks, failed);
            ProcessTasks(unknownTasks, unknown);

            return Ok(new List<Tuple<string, int[]>>() {
                new Tuple<string, int[]>("done", done),
                new Tuple<string, int[]>("failed", failed),
                new Tuple<string, int[]>("unmarked", unknown)
            });
        }

        [Route("DailyStats")]
        public IHttpActionResult Post([FromBody]DateTime time)
        {
            try
            {
                MongoClient client = new MongoClient();
                var db = client.GetDatabase("scheduler");
                var collection = db.GetCollection<Schedule>("schedules");

                var query = collection.AsQueryable().ToList().Where(x => x.UserEmail == User.Identity.Name).ToList();

                Schedule s = query.Where(x => x.Date.Date == time.Date).SingleOrDefault();

                return Ok(s);
            }
            catch(Exception e)
            {
                return InternalServerError(e);
            }
        }

        [Route("FindCommonTargets")]
        public async Task<IHttpActionResult> Post([FromBody] string targetName)
        {
            try
            {
                MongoClient client = new MongoClient();
                var db = client.GetDatabase("scheduler");
                var collection = db.GetCollection<TaskDone>("tasksDone");

                List<TaskDone> toSearch = await collection.FindAsync(Builders<TaskDone>.Filter.Where(x => x.IsPublic)).Result.ToListAsync();

                List<FuzzyStringComparisonOptions> options = new List<FuzzyStringComparisonOptions>();

                options.Add(FuzzyStringComparisonOptions.UseJaccardDistance);

                FuzzyStringComparisonTolerance tolerance = FuzzyString.FuzzyStringComparisonTolerance.Normal;

                List<TaskDone> toReturn = new List<TaskDone>();

                foreach (var task in toSearch)
                {
                    if (task.TaskName.ApproximatelyEquals(targetName, options, tolerance))
                    {
                        toReturn.Add(task);
                    }
                }

                return Ok(toReturn);
            }
            catch(Exception e)
            {
                return InternalServerError(e);
            }
        }

        [NonAction]
        private bool CanBuyAlgorithm(string name, int bonuses)
        {
            using (SqlConnection con = new SqlConnection(System.Web.Configuration.WebConfigurationManager.ConnectionStrings["DBCS"].ConnectionString))
            {
                SqlCommand cmd = new SqlCommand("spSubtractBonuses @n, @b", con);
                cmd.Parameters.AddWithValue("@n", name);
                cmd.Parameters.AddWithValue("@b", bonuses);
                con.Open();
                string res = (string)cmd.ExecuteScalar();

                return res == "ok";
            }
        }

        [Route("GetUserAlgoLog")]
        public async Task<IHttpActionResult> Put()
        {
            try
            {
                MongoClient client = new MongoClient();
                var db = client.GetDatabase("scheduler");
                var collection = db.GetCollection<AlgoLogEntry>("algorithmsLog");

                List<AlgoLogEntry> toReturn = new List<AlgoLogEntry>();

                toReturn = await collection.FindAsync(Builders<AlgoLogEntry>.Filter.Where(x => x.UserEmail == User.Identity.Name)).Result.ToListAsync();

                return Ok(toReturn.Where(x => x.EndTime > DateTime.Now).ToList());
            }
            catch(Exception e)
            {
                return InternalServerError(e);
            }
        }

        [NonAction]
        public void ProcessTasks(IEnumerable<SingleTask> tasks, int[] arr)
        {
            foreach (SingleTask task in tasks)
            {
                arr[(int)task.StartTime.TotalMinutes - 1]++;
                arr[(int)task.EndTime.TotalMinutes - 1]--;
            }

            int currValue = 0;

            for (int i = 0; i < arr.Length; ++i)
            {
                currValue += arr[i];
                arr[i] = currValue;
            }
        }
    }
}

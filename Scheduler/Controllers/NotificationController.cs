using MongoDB.Driver;
using Scheduler.Models.Auxiliary;
using Scheduler.Models.Custom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;

namespace Scheduler.Controllers
{
    [Authorize]
    [RoutePrefix("api/Notifications")]
    public class NotificationController : ApiController
    {

        [Route("GetNotifications")]
        public async Task<IHttpActionResult> Get()
        {
            try
            {
                MongoClient client = new MongoClient();
                var db = client.GetDatabase("scheduler");
                var collection = db.GetCollection<Notification>("notifications");

                //await PopulateDbAsync(collection);

                var toReturn = await collection.FindAsync(Builders<Notification>.Filter.Where(x => x.UserEmail == User.Identity.Name));

                return Ok(toReturn.ToList());
            }
            catch(Exception e)
            {
                return InternalServerError(e);
            }
        }

        [Route("Handle")]
        public async Task<IHttpActionResult> Post([FromBody]NotificationHandlerParams param)
        {
            try
            {
                NotificationHandler h1 = new TargetTaskNotificationHandler();
                NotificationHandler h2 = new FriendRequestNotificationHandler();

                h1.Next = h2;

                MongoClient client = new MongoClient();
                var db = client.GetDatabase("scheduler");
                var collection = db.GetCollection<Notification>("notifications");

                Notification n = collection.Find(Builders<Notification>.Filter.Where(x => x.Id == param.Id)).SingleOrDefault();

                if(n != null)
                {
                    h1.Accept(n, param.Status);

                    await collection.DeleteOneAsync(Builders<Notification>.Filter.Where(x => x.Id == n.Id));
                }
                else
                {
                    throw new Exception("Notification not found");
                }

                return Ok("Success");
            }
            catch(Exception e)
            {
                return InternalServerError(e);
            }
        }

        [AllowAnonymous]
        [Route("AddNewMessageNotification")]
        public async Task<IHttpActionResult> Post([FromBody]Notification n)
        {
            try
            {
                MongoClient client = new MongoClient();
                var db = client.GetDatabase("scheduler");
                var collection = db.GetCollection<Notification>("notifications");

                var query = await collection.FindAsync(Builders<Notification>.Filter.Where(x => x.Title == n.Title));
                Notification not = query.FirstOrDefault();

                if (not == null)
                {
                    await collection.InsertOneAsync(n);
                }

                return Ok("Success");
            }
            catch(Exception e)
            {
                return InternalServerError(e);
            }
        }

        private async Task PopulateDbAsync(IMongoCollection<Notification> collection)
        {
            List<Notification> toAdd = new List<Notification>()
            {
                new Notification()
                {
                     Title = "Target1",
                     Body = "Description of target1",
                     Seen = false,
                     SendingTime = DateTime.Now,
                     Type = "TargetTask",
                     UserEmail = User.Identity.Name
                },
                new Notification()
                {
                    Title = "Target2",
                    Body = "You didn't work on this target yesterday",
                    Seen = false,
                    SendingTime = DateTime.Now,
                    Type = "OmmitedTask",
                    UserEmail = User.Identity.Name
                },
                new Notification()
                {
                    Title = "Target3",
                    Body = "Congrats! You finished this task",
                    Seen = false,
                    SendingTime = DateTime.Now,
                    Type = "TaskDone",
                    UserEmail = User.Identity.Name
                }
            };

            await collection.InsertManyAsync(toAdd);
        }
    }
}

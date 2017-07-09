using MongoDB.Driver;
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
    [RoutePrefix("api/Dialogues")]
    public class DialoguesController : ApiController
    {
        [Route("GetDialogue")]
        public async Task<IHttpActionResult> Get([FromUri] string mail)
        {
            try
            {
                MongoClient client = new MongoClient();
                var db = client.GetDatabase("scheduler");
                var collection = db.GetCollection<Dialogue>("dialogues");

                var toReturn = await collection.FindAsync(Builders<Dialogue>.Filter.Where(x => (x.Writer1.Email == User.Identity.Name && x.Writer2.Email == mail) || x.Writer1.Email == mail && x.Writer2.Email == User.Identity.Name)).Result.ToListAsync();

                return Ok(toReturn.FirstOrDefault());
            }
            catch(Exception e)
            {
                return InternalServerError(e);
            }
        }

        [Route("GetMessages")]
        public async Task<IHttpActionResult> Put([FromBody] string id)
        {
            try
            {
                MongoClient client = new MongoClient();
                var db = client.GetDatabase("scheduler");
                var collection = db.GetCollection<Message>("messsages");

                List<Message> messages = await collection.FindAsync(Builders<Message>.Filter.Where(x => x.DialogueId == id)).Result.ToListAsync();

                return Ok(messages.OrderByDescending(x => x.SendingTime).ToList());
            }
            catch(Exception e)
            {
                return InternalServerError(e);
            }
        }

        [Route("AddMessage")]
        public async Task<IHttpActionResult> Post([FromBody] Message m)
        {
            try
            {
                MongoClient client = new MongoClient();
                var db = client.GetDatabase("scheduler");
                var collection = db.GetCollection<Message>("messages");

                await collection.InsertOneAsync(m);

                return Ok("Success");
            }
            catch(Exception e)
            {
                return InternalServerError(e);
            }
        }

        [Route("AddDialogue")]
        public async Task<IHttpActionResult> Post([FromBody] Dialogue d)
        {
            try
            {
                MongoClient client = new MongoClient();
                var db = client.GetDatabase("scheduler");
                var collection = db.GetCollection<Dialogue>("dialogues");

                await collection.InsertOneAsync(d);

                return Ok("Success");
            }
            catch(Exception e)
            {
                return InternalServerError(e);
            }
        }
    }
}

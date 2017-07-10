using MongoDB.Driver;
using Scheduler.Models.Auxiliary;
using Scheduler.Models.Custom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
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
            catch (Exception e)
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
                var collection = db.GetCollection<Message>("messages");

                List<Message> messages = await collection.FindAsync(Builders<Message>.Filter.Where(x => x.DialogueId == id)).Result.ToListAsync();

                return Ok(messages.OrderByDescending(x => x.SendingTime).ToList());
            }
            catch (Exception e)
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

                var dialogues = db.GetCollection<Dialogue>("dialogues");

                var needed = await dialogues.FindAsync(Builders<Dialogue>.Filter.Where(x => x.Id == m.DialogueId)).Result.ToListAsync();

                Dialogue d = needed.FirstOrDefault();
                d.LastMessageSender = m.SenderMail;
                d.LastMessageTime = m.SendingTime;

                await dialogues.FindOneAndReplaceAsync(Builders<Dialogue>.Filter.Where(x => x.Id == d.Id), d);

                return Ok("Success");
            }
            catch (Exception e)
            {
                return InternalServerError(e);
            }
        }

        [Route("RemoveDialogue")]
        public async Task<IHttpActionResult> Delete([FromBody]RemoveDialogueParams param)
        {
            try
            {
                MongoClient client = new MongoClient();
                var db = client.GetDatabase("scheduler");
                var dialogues = db.GetCollection<Dialogue>("dialogues");

                var d = await dialogues.FindAsync(Builders<Dialogue>.Filter.Where
                    (x => x.Id == param.DialogueId));

                Dialogue needed = d.FirstOrDefault();

                if(User.Identity.Name == needed.Writer1.Email)
                {
                    needed.Writer1.LastDeletionTime = param.LastDeletionTime;
                }
                else
                {
                    needed.Writer2.LastDeletionTime = param.LastDeletionTime;
                }

                await dialogues.FindOneAndReplaceAsync(Builders<Dialogue>.Filter.Where(x => x.Id == needed.Id), needed);

                return Ok("Success");
            }
            catch(Exception e)
            {
                return InternalServerError(e);
            }
        }

        [Route("GetDialoguesInfo")]
        public async Task<IHttpActionResult> Get()
        {
            try
            {
                List<Tuple<Dialogue, UserInfo>> toReturn = new List<Tuple<Dialogue, UserInfo>>();

                MongoClient client = new MongoClient();
                var db = client.GetDatabase("scheduler");
                var dialogues = db.GetCollection<Dialogue>("dialogues");

                List<Dialogue> componentOne = dialogues.AsQueryable().Where(x => x.Writer1.Email == User.Identity.Name || x.Writer2.Email == User.Identity.Name).ToList();

                componentOne = componentOne.Where(x =>
                (x.Writer1.Email == User.Identity.Name &&  x.LastMessageTime > x.Writer1.LastDeletionTime) ||
                (x.Writer2.Email == User.Identity.Name && x.LastMessageTime > x.Writer2.LastDeletionTime)).ToList();

                for (int i = 0; i < componentOne.Count(); ++i)
                {
                    string mail = componentOne[i].Writer1.Email != User.Identity.Name ? componentOne[i].Writer1.Email : componentOne[i].Writer2.Email;

                    using (HttpClient cl = new HttpClient())
                    {
                        cl.DefaultRequestHeaders.Clear();
                        cl.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                        cl.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", Request.Headers.Authorization.Parameter);
                        cl.BaseAddress = new Uri("http://localhost:24082");

                        HttpResponseMessage msg = await cl.GetAsync("/api/UserInfo/GetInfo?email=" + mail);

                        if (msg.IsSuccessStatusCode)
                        {
                            UserInfo ui = msg.Content.ReadAsAsync<UserInfo>().Result;

                            toReturn.Add(new Tuple<Dialogue, UserInfo>(componentOne[i], ui));
                        }
                    }
                }

                return Ok(toReturn);
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

                using (HttpClient c = new HttpClient())
                {
                    c.DefaultRequestHeaders.Clear();
                    c.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    c.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", Request.Headers.Authorization.Parameter);
                    c.BaseAddress = new Uri("http://localhost:24082");
                    HttpResponseMessage msg = c.GetAsync("/api/UserInfo/GetInfo?email=" + d.Writer1.Email).Result;

                    if (msg.IsSuccessStatusCode)
                    {
                        UserInfo ui = msg.Content.ReadAsAsync<UserInfo>().Result;

                        d.Writer1.Name = ui.FirstName;
                        d.Writer1.Surname = ui.LastName;
                    }
                }

                using (HttpClient c = new HttpClient())
                {
                    c.DefaultRequestHeaders.Clear();
                    c.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    c.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", Request.Headers.Authorization.Parameter);
                    c.BaseAddress = new Uri("http://localhost:24082");
                    HttpResponseMessage msg = c.GetAsync("/api/UserInfo/GetInfo?email=" + d.Writer2.Email).Result;

                    if (msg.IsSuccessStatusCode)
                    {
                        UserInfo ui = msg.Content.ReadAsAsync<UserInfo>().Result;

                        d.Writer2.Name = ui.FirstName;
                        d.Writer2.Surname = ui.LastName;
                    }
                }

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

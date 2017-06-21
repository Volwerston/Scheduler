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
    [RoutePrefix("api/Idea")]
    public class IdeaController : ApiController
    {
        
        [Route("Add")]
        public async Task<IHttpActionResult> Post([FromBody]Idea idea)
        {
            try
            {
                MongoClient client = new MongoClient();
                var db = client.GetDatabase("scheduler");
                var ideas = db.GetCollection<Idea>("ideas");

                await ideas.InsertOneAsync(idea);

                return Ok("Success");
            }
            catch(Exception e)
            {
                return InternalServerError(e);
            }
        }

        [Route("GetChunk")]
        public async Task<IHttpActionResult> Post([FromBody]IdeasChunkParams param)
        {
            try
            {
                MongoClient client = new MongoClient();
                IMongoDatabase db = client.GetDatabase("scheduler");
                IMongoCollection<Idea> ideas = db.GetCollection<Idea>("ideas");

                var toReturn = await ideas.FindAsync(
                    Builders<Idea>.Filter.Where(x => x.TargetId == param.TargetId));

                return Ok(toReturn.ToList().Skip(param.EntitiesNum * (param.ChunkNumber - 1)).Take(param.EntitiesNum));
            }
            catch(Exception e)
            {
                return InternalServerError(e);
            }
        }

        [Route("Count")]
        public async Task<IHttpActionResult> Get([FromUri]string id)
        {
            try
            {
                MongoClient client = new MongoClient();
                var db = client.GetDatabase("scheduler");
                var ideas = db.GetCollection<Idea>("ideas");
                var entities = await ideas.FindAsync(Builders<Idea>.Filter.Where(x => x.TargetId == id));
                var res = entities.ToList().Count();
                return Ok(res);
            }
            catch(Exception e)
            {
                return InternalServerError(e);
            }
        }

        [Route("Delete")]
        public async Task<IHttpActionResult> Delete([FromUri]string id)
        {
            try
            {
                MongoClient client = new MongoClient();
                var db = client.GetDatabase("scheduler");
                var ideas = db.GetCollection<Idea>("ideas");
                await ideas.DeleteOneAsync(Builders<Idea>.Filter.Where(x => x.Id == id));
                return Ok("Success");
            }
            catch (Exception e)
            {
                return InternalServerError(e);
            }
        }

    }
}

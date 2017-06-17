using Algorithms;
using MongoDB.Bson;
using MongoDB.Driver;
using Scheduler.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace Scheduler.Controllers
{
    public class TargetsToSave
    {
        public List<string> TargetNames { get; set; }
        public List<DbTarget> Targets { get; set; }
    }

    [Authorize]
    [RoutePrefix("api/Targets")]
    public class TargetsController : ApiController
    {

        public IHttpActionResult Get([FromUri]string id)
        {
            try
            {
                MongoClient client = new MongoClient();
                var db = client.GetDatabase("scheduler");
                var collection = db.GetCollection<DbTarget>("targets");


                DbTarget toReturn = collection.Find(Builders<DbTarget>.Filter.Where(x => x.Id == id && x.UserEmail == User.Identity.Name)).FirstOrDefault();

                return Ok(toReturn);
            }
            catch(Exception e)
            {
                return InternalServerError(e);
            }
        }

        [Route("Post")]
        [HttpPost]
        public IHttpActionResult Post([FromBody]List<Target> targets)
        {
            try
            {
                int[,] adj = Algorithms.Algorithms.GetAdjacencyMatrix(targets);
                if (Algorithms.Algorithms.ContainsCycle(adj, new List<int>(), new List<List<int>>(), 0))
                {
                    throw new Exception("Target graph must not contain cycles");
                }

                TargetNode root = Algorithms.Algorithms.BuildTargetTree(targets);
                List<TargetNode> nodes = Algorithms.Algorithms.TopologicalSort(root);

                return Ok(nodes.Select(x => new
                {
                    Name = x.Title
                }));
            }
            catch (Exception e)
            {
                return InternalServerError(e);
            }
        }

        [Route("Search")]
        public IHttpActionResult Post([FromBody]TargetSearchOptions options)
        {
            try
            {
                MongoClient client = new MongoClient();
                var db = client.GetDatabase("scheduler");
                var collection = db.GetCollection<DbTarget>("targets");

                IEnumerable<DbTarget> query = null;

                if (String.IsNullOrEmpty(options.Title))
                {
                    query = collection.AsQueryable().ToList()
                        .Where(
                    x => x.UserEmail == options.UserName
                    && String.Compare(x.Id, options.LastObjectId, true) > 0
                    ).Take(10);

                }
                else
                {
                    query = collection.AsQueryable().ToList().Where(
                        x => x.Name.ToLower().Contains(options.Title.ToLower())
                        && x.UserEmail == options.UserName
                        && String.Compare(x.Id,options.LastObjectId,true) > 0
                        ).Take(10);
                }

                if (options.OrderBy == "date")
                {
                    var toReturn = query
                        .Select(x => new
                        {
                            Name = x.Name,
                            Difficulty = x.Difficulty,
                            StartDate = x.StartDate,
                            Solution = x.Solution,
                            Id = x.Id
                        })
                        .OrderByDescending(x => x.StartDate)
                        .ToList();

                    return Ok(toReturn);
                }
                else
                {
                    var toReturn = query
                        .Select(x => new
                        {
                            Name = x.Name,
                            Difficulty = x.Difficulty,
                            StartDate = x.StartDate,
                            Solution = x.Solution,
                            Id = x.Id
                        })
                        .OrderByDescending(y => y.Difficulty)
                        .ToList();

                    return Ok(toReturn);
                }

            }
            catch (Exception e)
            {
                return InternalServerError(e);
            }
        }

        [Route("PostToSave")]
        [HttpPost]
        public IHttpActionResult Post([FromBody]TargetsToSave toSave)
        {
            try
            {
                List<DbTarget> save = new List<DbTarget>();

                for (int i = 0; i < toSave.TargetNames.Count(); ++i)
                {
                    for (int j = 0; j < toSave.Targets.Count(); ++j)
                    {
                        if (toSave.Targets[j].Name == toSave.TargetNames[i])
                        {
                            save.Add(toSave.Targets[j]);
                            break;
                        }
                    }
                }

                save[0].WeekendsRemained = save[0].Difficulty;
                for (int i = 1; i < save.Count(); ++i)
                {
                    save[i - 1].NextTarget = save[i];
                    save[i].WeekendsRemained = save[i].Difficulty;
                }

                MongoClient client = new MongoClient();
                var db = client.GetDatabase("scheduler");
                var collection = db.GetCollection<DbTarget>("targets");

                collection.InsertOne(save[0]);

                return Ok();
            }
            catch (Exception e)
            {
                return InternalServerError(e);
            }
        }
    }
}

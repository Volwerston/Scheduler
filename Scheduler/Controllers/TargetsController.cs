using Algorithms;
using MongoDB.Bson;
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
    public class TargetsToSave
    {
        public List<string> TargetNames { get; set; }
        public List<DbTarget> Targets { get; set; }
        public List<string> Tags { get; set; }
        public string Title { get; set; }
    }

    [Authorize]
    [RoutePrefix("api/Targets")]
    public class TargetsController : ApiController
    {

        public IHttpActionResult Get([FromUri]string facadeId)
        {
            try
            {
                MongoClient client = new MongoClient();
                var db = client.GetDatabase("scheduler");

                var facadeCollection = db.GetCollection<TargetFacade>("targetFacades");
                var collection = db.GetCollection<DbTarget>("targets");

                var target = facadeCollection.Find(Builders<TargetFacade>.Filter.Where(x => x.Id == facadeId)).FirstOrDefault();

                if (target == null)
                {
                    throw new Exception("Target not found");
                }

                DbTarget toReturn = collection.Find(Builders<DbTarget>.Filter.Where(x => x.Id == target.TargetId && x.UserEmail == User.Identity.Name)).FirstOrDefault();

                if (toReturn == null)
                {
                    throw new Exception("Target not found");
                }

                return Ok(toReturn);
            }
            catch (Exception e)
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
                var collection = db.GetCollection<TargetFacade>("targetFacades");

                List<TargetFacade> query = null;
                if (options.Title == null) options.Title = "";

                if (String.IsNullOrEmpty(options.Title) && (options.Tags == null || options.Tags.Count() == 0))
                {
                    query = collection.AsQueryable().ToList()
                        .Where(
                    x => x.UserEmail == options.UserName
                    && String.Compare(x.Id, options.LastObjectId, true) > 0
                    ).Take(10).ToList();

                }
                else
                {
                    if (options.Title.Trim() == "")
                    {
                        query = collection.AsQueryable().ToList().Where(
               x => (x.Tags.Intersect(options.Tags).Count() != 0)
                    && x.UserEmail == options.UserName
                    && String.Compare(x.Id, options.LastObjectId, true) > 0
                    ).Take(10).ToList();
                    }
                    else if (options.Tags == null || options.Tags.Count() == 0)
                    {
                        query = collection.AsQueryable().ToList().Where(
                                 x => x.Title.Trim().ToLower().Contains(options.Title.Trim().ToLower())
                                    && x.UserEmail == options.UserName
                                    && String.Compare(x.Id, options.LastObjectId, true) > 0
                                    ).Take(10).ToList();
                    }
                    else
                    {
                        query = collection.AsQueryable().ToList().Where(
                                     x => (x.Title.Trim().ToLower().Contains(options.Title.Trim().ToLower())
                                         || x.Tags.Intersect(options.Tags).Count() != 0)
                                         && x.UserEmail == options.UserName
                                         && String.Compare(x.Id, options.LastObjectId, true) > 0
                                           ).Take(10).ToList();
                    }
                }

                if (options.OrderBy == "date")
                {
                    if (query.Count() != 0)
                    {
                        query = query.OrderByDescending(x => x.StartDate).ToList();
                        string maxId = query.Max(x => x.Id);
                        query[query.Count() - 1].Id = maxId;
                    }

                    var toReturn = query
                        .Select(x => new
                        {
                            Name = x.Title,
                            Difficulty = x.Difficulty,
                            StartDate = x.StartDate,
                            Id = x.Id
                        })
                        .ToList();

                    return Ok(toReturn);
                }
                else
                {
                    if (query.Count() != 0)
                    {
                        query = query.OrderByDescending(y => y.Difficulty).ToList();
                        string maxId = query.Max(x => x.Id);
                        query[query.Count() - 1].Id = maxId;
                    }

                    var toReturn = query
                        .Select(x => new
                        {
                            Name = x.Title,
                            Difficulty = x.Difficulty,
                            StartDate = x.StartDate,
                            Id = x.Id
                        })
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
        public async Task<IHttpActionResult> Post([FromBody]TargetsToSave toSave)
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

                await collection.InsertOneAsync(save[0]);

                DbTarget selected = collection.Find(Builders<DbTarget>.Filter.Where(x => x.UserEmail == User.Identity.Name && x.Name == save[0].Name)).Single();

                TargetFacade f = new TargetFacade()
                {
                    Tags = toSave.Tags,
                    Title = toSave.Title,
                    TargetId = selected.Id,
                    Difficulty = save[0].Difficulty,
                    StartDate = save[0].StartDate,
                    UserEmail = User.Identity.Name
                };

                IMongoCollection<TargetFacade> c = db.GetCollection<TargetFacade>("targetFacades");
                await c.InsertOneAsync(f);

                return Ok("Success");
            }
            catch (Exception e)
            {
                return InternalServerError(e);
            }
        }
    }
}

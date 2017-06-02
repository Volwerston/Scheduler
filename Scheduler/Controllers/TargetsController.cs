using Algorithms;
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

        [Route("Post")]
        [HttpPost]
        public IHttpActionResult Post([FromBody]List<Target> targets){
            try
            {
                int[,] adj = Algorithms.Algorithms.GetAdjacencyMatrix(targets);
                if(Algorithms.Algorithms.ContainsCycle(adj, new List<int>(),new List<List<int>>(),0))
                {
                    throw new Exception("Target graph must not contain cycles");
                }

                TargetNode root = Algorithms.Algorithms.BuildTargetTree(targets);
                List<TargetNode> nodes = Algorithms.Algorithms.TopologicalSort(root);

                return Ok(nodes.Select(x => new {
                    Name = x.Title
                }));
            }
            catch(Exception e)
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

                for (int i = 1; i < save.Count(); ++i)
                {
                    save[i - 1].NextTarget = save[i];
                }

                MongoClient client = new MongoClient();
                var db = client.GetDatabase("scheduler");
                var collection = db.GetCollection<DbTarget>("targets");

                collection.InsertOne(save[0]);

                return Ok();
            }
            catch(Exception e)
            {
                return InternalServerError(e);
            }
        }
    }
}

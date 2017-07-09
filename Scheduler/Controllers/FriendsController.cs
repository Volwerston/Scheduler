using MongoDB.Driver;
using Scheduler.Hubs;
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
    [RoutePrefix("api/Friends")]
    public class FriendsController : ApiController
    {
        [Route("UserFriends")]
        public async Task<IHttpActionResult> Get()
        {
            List<FriendPair> pairs = new List<FriendPair>();

            try
            {
                using (SqlConnection con = new SqlConnection(System.Web.Configuration.WebConfigurationManager.ConnectionStrings["DBCS"].ConnectionString))
                {
                    SqlCommand cmd = new SqlCommand("Select FriendMail1, FriendMail2 from Friends Where FriendMail1 = @mail OR FriendMail2 = @mail Union Select SenderMail as FriendMail1, RecipientMail as FriendMail2 from FriendRequests Where RecipientMail = @mail", con);
                    cmd.Parameters.AddWithValue("@mail", User.Identity.Name);

                    con.Open();
                    using (SqlDataReader rdr = await cmd.ExecuteReaderAsync())
                    {
                        while (rdr.Read())
                        {
                            FriendPair p = new FriendPair()
                            {
                                FriendMail1 = rdr["FriendMail1"].ToString(),
                                FriendMail2 = rdr["FriendMail2"].ToString()  
                            };

                            pairs.Add(p);
                        }
                    }
                }

                return Ok(pairs);
            }
            catch(Exception e)
            {
                return InternalServerError(e);
            }
        }

        [Route("GetAllUserFriendsInfo")]
        public async Task<IHttpActionResult> Post()
        {
            List<UserInfo> toReturn = new List<UserInfo>();

            try
            {
                using (SqlConnection con = new SqlConnection(System.Web.Configuration.WebConfigurationManager.ConnectionStrings["DBCS"].ConnectionString))
                {
                    SqlCommand cmd = new SqlCommand("spGetAllUserFriendsInfo @mail", con);
                    cmd.Parameters.AddWithValue("@mail", User.Identity.Name);

                    con.Open();
                    using (SqlDataReader rdr = await cmd.ExecuteReaderAsync())
                    {
                        while (rdr.Read()) {
                            UserInfo i = new UserInfo()
                            {
                                Avatar = rdr["Avatar"] == DBNull.Value ? null : (byte[])rdr["Avatar"],
                                Country = rdr["Country"] == DBNull.Value ? "-" : rdr["Country"].ToString(),
                                FirstName = rdr["FirstName"] == DBNull.Value ? "-" : rdr["FirstName"].ToString(),
                                Interests = rdr["Interests"] == DBNull.Value ? "-" : rdr["Interests"].ToString(),
                                LastName = rdr["LastName"] == DBNull.Value ? "-" : rdr["LastName"].ToString(),
                                Region = rdr["Region"] == DBNull.Value ? "-" : rdr["Region"].ToString(),
                                Settlement = rdr["Settlement"] == DBNull.Value ? "-" : rdr["Settlement"].ToString(),
                                Profession = rdr["Profession"] == DBNull.Value ? "-" : rdr["Profession"].ToString(),
                                Email = rdr["UserEmail"].ToString(),
                                IsOnline = AppHub.IsUserOnline(rdr["UserEmail"].ToString())
                            };

                            toReturn.Add(i);
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

        [Route("GetUserSentRequests")]
        public async Task<IHttpActionResult> Put()
        {
            List<FriendRequest> requests = new List<FriendRequest>();

            try
            {
                using (SqlConnection con = new SqlConnection(System.Web.Configuration.WebConfigurationManager.ConnectionStrings["DBCS"].ConnectionString))
                {
                    SqlCommand cmd = new SqlCommand("Select * from FriendRequests where SenderMail=@mail", con);
                    cmd.Parameters.AddWithValue("@mail", User.Identity.Name);

                    con.Open();
                    using (SqlDataReader rdr = await cmd.ExecuteReaderAsync())
                    {
                        while (rdr.Read())
                        {
                            FriendRequest r = new FriendRequest()
                            {
                                 RecipientMail = rdr["RecipientMail"].ToString(),
                                 SenderMail = User.Identity.Name,
                                 SendingTime = Convert.ToDateTime(rdr["SendingTime"].ToString())
                            };

                            requests.Add(r);                
                        }
                    }
                }

                return Ok(requests);
            }
            catch(Exception e)
            {
                return InternalServerError(e);
            }
        }

        [Route("DeleteUserSentRequest")]
        public async Task<IHttpActionResult> Delete([FromUri] string email)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(System.Web.Configuration.WebConfigurationManager.ConnectionStrings["DBCS"].ConnectionString))
                {
                    SqlCommand cmd = new SqlCommand("Delete from FriendRequests Where SenderMail = @mail1 AND RecipientMail = @mail2", con);
                    cmd.Parameters.AddWithValue("@mail1", User.Identity.Name);
                    cmd.Parameters.AddWithValue("@mail2", email);

                    con.Open();
                    await cmd.ExecuteNonQueryAsync();
                }

                return Ok("Success");
            }
            catch(Exception e)
            {
                return InternalServerError(e);
            }
        }

        [Route("AddUserRequest")]
        public async Task<IHttpActionResult> Post([FromUri] string email)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(System.Web.Configuration.WebConfigurationManager.ConnectionStrings["DBCS"].ConnectionString))
                {
                    SqlCommand cmd = new SqlCommand("Insert Into FriendRequests Values(@mail1, @mail2, GETDATE())", con);
                    cmd.Parameters.AddWithValue("@mail1", User.Identity.Name);
                    cmd.Parameters.AddWithValue("@mail2", email);

                    con.Open();
                    await cmd.ExecuteNonQueryAsync();
                }

                MongoClient client = new MongoClient();
                var db = client.GetDatabase("scheduler");
                var collection = db.GetCollection<Notification>("notifications");

                Notification toInsert = new Notification()
                {
                     Title = "Friend Request",
                     Body = "<a href=\"/Main/ExternalAccountPage?email=" + User.Identity.Name + "\"> This user </a> wants to become your friend",
                     Type = "FriendRequest_" + User.Identity.Name,
                     Seen = false,
                     UserEmail = email,
                     SendingTime = DateTime.Now 
                };

                await collection.InsertOneAsync(toInsert);

                return Ok("Success");
            }
            catch(Exception e)
            {
                return InternalServerError(e);
            }
        }

        [Route("GetPossibleFriends")]
        public async Task<IHttpActionResult> Put([FromBody] string email)
        {
            List<Tuple<UserInfo, int>> toReturn = new List<Tuple<UserInfo, int>>();

            try
            {
                using (SqlConnection con = new SqlConnection(System.Web.Configuration.WebConfigurationManager.ConnectionStrings["DBCS"].ConnectionString))
                {
                    SqlCommand cmd = new SqlCommand("spGetPossibleFriends @mail", con);
                    cmd.Parameters.AddWithValue("@mail", email);

                    con.Open();

                    using (SqlDataReader rdr = await cmd.ExecuteReaderAsync())
                    {
                        while (rdr.Read())
                        {
                            toReturn.Add(new Tuple<UserInfo, int>(new UserInfo() {
                                 Avatar = rdr["Avatar"] != DBNull.Value ? (byte[])rdr["Avatar"] : null,
                                 Email = rdr["UserEmail"].ToString(),
                                 FirstName = rdr["FirstName"].ToString(),
                                 LastName = rdr["LastName"].ToString(),
                                 IsOnline = AppHub.IsUserOnline(rdr["UserEmail"].ToString())                    
                            },
                            Convert.ToInt32(rdr["FriendsCount"].ToString())
                            ));
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
    }
}

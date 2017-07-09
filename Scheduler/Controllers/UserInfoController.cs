using Scheduler.Models.Auxiliary;
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
    [RoutePrefix("api/UserInfo")]
    public class UserInfoController : ApiController
    {
        [Route("PostProfile")]
        public async Task<IHttpActionResult> Post([FromBody] UserInfo toSave)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(System.Web.Configuration.WebConfigurationManager.ConnectionStrings["DBCS"].ConnectionString))
                {
                    SqlCommand cmd = new SqlCommand("Update UserInfo set Country=@c, Region=@r, TimeZone = @tz, TimeZoneDescription = @td, Settlement=@s, Profession=@p, Interests=@i where UserEmail=@e", con);
                    cmd.Parameters.AddWithValue("@c", toSave.Country == null ? "-" : toSave.Country);
                    cmd.Parameters.AddWithValue("@r", toSave.Region == null ? "-" : toSave.Region);
                    cmd.Parameters.AddWithValue("@tz", toSave.TimeZone);
                    cmd.Parameters.AddWithValue("@td", toSave.TimeZoneDescription);
                    cmd.Parameters.AddWithValue("@s", toSave.Settlement == null ? "-" : toSave.Settlement);
                    cmd.Parameters.AddWithValue("@p", toSave.Profession == null ? "-" : toSave.Profession);
                    cmd.Parameters.AddWithValue("@i", toSave.Interests == null ? "-" : toSave.Interests);
                    cmd.Parameters.AddWithValue("@e", User.Identity.Name);

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

        [Route("FindUserPosts")]
        public async Task<IHttpActionResult> Post([FromBody] FindUserPostsParams p)
        {
            List<UserPost> toReturn = new List<UserPost>();

            try
            {
                using (SqlConnection con = new SqlConnection(System.Web.Configuration.WebConfigurationManager.ConnectionStrings["DBCS"].ConnectionString))
                {
                    SqlCommand cmd = new SqlCommand("Select Top 10 AddingTime, Text, Id from UserPosts Where UserEmail=@m and Id>@id Order by AddingTime Desc", con);
                    cmd.Parameters.AddWithValue("@m", p.Email);
                    cmd.Parameters.AddWithValue("@id", p.LastId);

                    con.Open();
                    using (SqlDataReader rdr = await cmd.ExecuteReaderAsync())
                    {
                        while(rdr.Read())
                        {
                            UserPost p1 = new UserPost()
                            {
                                AddingTime = Convert.ToDateTime(rdr["AddingTime"].ToString()),
                                Text = rdr["Text"].ToString(),
                                Id = Convert.ToInt32(rdr["Id"].ToString())
                            };

                            toReturn.Add(p1);                         
                        }
                    }
                }

                return Ok(toReturn);
            }
            catch (Exception e)
            {
                return InternalServerError(e);
            }
        }

        
        [Route("DeletePost")]
        public async Task<IHttpActionResult> Delete([FromUri] int id)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(System.Web.Configuration.WebConfigurationManager.ConnectionStrings["DBCS"].ConnectionString))
                {
                    SqlCommand cmd = new SqlCommand("Delete from UserPosts Where id = @id", con);
                    cmd.Parameters.AddWithValue("@id", id);

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

        [Route("PostAvatar")]
        public async Task<IHttpActionResult> Post([FromBody] byte[] toSave)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(System.Web.Configuration.WebConfigurationManager.ConnectionStrings["DBCS"].ConnectionString))
                {
                    SqlCommand cmd = new SqlCommand("Update UserInfo Set Avatar=@ava Where UserEmail=@mail", con);
                    cmd.Parameters.AddWithValue("@ava", toSave);
                    cmd.Parameters.AddWithValue("@mail", User.Identity.Name);

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

        [Route("AddUserPost")]
        public async Task<IHttpActionResult> Post([FromBody] UserPost post)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(System.Web.Configuration.WebConfigurationManager.ConnectionStrings["DBCS"].ConnectionString))
                {
                    SqlCommand cmd = new SqlCommand("INSERT INTO UserPosts VALUES(@t, @u, @a)", con);
                    cmd.Parameters.AddWithValue("@t", post.Text);
                    cmd.Parameters.AddWithValue("@u", post.UserEmail);
                    cmd.Parameters.AddWithValue("@a", DateTime.Now);

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

        [Route("GetInfo")]
        public async Task<IHttpActionResult> Get([FromUri]string email)
        {
            try
            {
                UserInfo toReturn = new UserInfo();

                using (SqlConnection con = new SqlConnection(System.Web.Configuration.WebConfigurationManager.ConnectionStrings["DBCS"].ConnectionString))
                {
                    SqlCommand cmd = new SqlCommand(@"SELECT Avatar, Country, Region, Settlement, Profession, 
                                                      Interests, FirstName, LastName, TimeZone, TimeZoneDescription
                                                      FROM UserInfo
                                                      INNER JOIN AspNetUsers 
                                                      ON UserInfo.UserEmail = AspNetUsers.Email
                                                      WHERE EMAIL=@mail", con);

                    cmd.Parameters.AddWithValue("@mail", email);

                    con.Open();
                    using (SqlDataReader rdr = await cmd.ExecuteReaderAsync())
                    {
                        if (rdr.Read())
                        {
                            toReturn.FirstName = rdr["FirstName"].ToString();
                            toReturn.LastName = rdr["LastName"].ToString();
                            toReturn.Avatar = rdr["Avatar"] != DBNull.Value ? (byte[])rdr["Avatar"] : null;
                            toReturn.Country = rdr["Country"] != DBNull.Value ? rdr["Country"].ToString() : "-";
                            toReturn.Region = rdr["Region"] != DBNull.Value ? rdr["Region"].ToString() : "-";
                            toReturn.Settlement = rdr["Settlement"] != DBNull.Value ? rdr["Settlement"].ToString() : "-";
                            toReturn.Profession = rdr["Profession"] != DBNull.Value ? rdr["Profession"].ToString() : "-";
                            toReturn.Interests = rdr["Interests"] != DBNull.Value ? rdr["Interests"].ToString() : "-";
                            toReturn.TimeZone = rdr["TimeZone"] != DBNull.Value ? Convert.ToInt32(rdr["TimeZone"].ToString()) : 0;
                            toReturn.TimeZoneDescription = rdr["TimeZoneDescription"] != DBNull.Value ? rdr["TimeZoneDescription"].ToString() : "-";
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

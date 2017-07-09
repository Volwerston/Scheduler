using MongoDB.Bson;
using Newtonsoft.Json;
using Scheduler.Models;
using Scheduler.Models.Auxiliary;
using Scheduler.Models.Custom;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Xml.Serialization;

namespace Scheduler.Controllers
{
    public class MainController : Controller
    {

        public ActionResult StartPage()
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("AccountPage");
            }

            using (FileStream stream = new FileStream(Server.MapPath("~/Common/Text files/start_blocks.xml"), FileMode.Open, FileAccess.Read))
            {
                XmlSerializer ser = new XmlSerializer(typeof(List<StartPageBlockData>));
                List<StartPageBlockData> data = (List<StartPageBlockData>)ser.Deserialize(stream);
                ViewBag.BlocksData = data;
            }

            return View();
        }

        public ActionResult Register()
        {
            RegisterBindingModel model = new RegisterBindingModel();
            return View(model);
        }

        public ActionResult ConfirmMail(string token)
        {
            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri("http://localhost:24082/");
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                HttpResponseMessage message = client.GetAsync("api/Account/RegisterConfirmation?token=" + token.ToString()).Result;

                if (message.IsSuccessStatusCode)
                {
                    return View("MailConfirmSuccess");
                }
            }

            return View("MailConfirmError");
        }

        [Authorize]
        public ActionResult AccountPage()
        {
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", Request.Cookies["access_token"].Value);
                client.BaseAddress = new Uri("http://localhost:24082");

                HttpResponseMessage msg = client.GetAsync("/api/UserInfo/GetInfo?email=" + User.Identity.Name).Result;

                if (msg.IsSuccessStatusCode)
                {
                    UserInfo info = msg.Content.ReadAsAsync<UserInfo>().Result;

                    if (info.Avatar == null)
                    {
                        using (FileStream s = new FileStream(Server.MapPath("~/Common/Images/empty_avatar.jpg"), FileMode.Open, FileAccess.Read))
                        {
                            info.Avatar = new byte[s.Length];
                            s.Read(info.Avatar, 0, (int)s.Length);
                        }
                    }

                    ViewData["avatar"] = String.Format("data:image/jpg;base64,{0}", Convert.ToBase64String(info.Avatar));

                    return View(info);
                }
            }
            return View("Error");
        }

        [Authorize]
        public ActionResult ExternalAccountPage(string email)
        {
            if (email == User.Identity.Name) return RedirectToAction("AccountPage");

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", Request.Cookies["access_token"].Value);
                client.BaseAddress = new Uri("http://localhost:24082");

                HttpResponseMessage msg = client.GetAsync("/api/UserInfo/GetInfo?email=" + email).Result;

                if (msg.IsSuccessStatusCode)
                {
                    UserInfo info = msg.Content.ReadAsAsync<UserInfo>().Result;

                    if (info.Avatar == null)
                    {
                        using (FileStream s = new FileStream(Server.MapPath("~/Common/Images/empty_avatar.jpg"), FileMode.Open, FileAccess.Read))
                        {
                            info.Avatar = new byte[s.Length];
                            s.Read(info.Avatar, 0, (int)s.Length);
                        }
                    }

                    ViewData["email"] = email;
                    ViewData["avatar"] = String.Format("data:image/jpg;base64,{0}", Convert.ToBase64String(info.Avatar));

                    return View(info);
                }
            }
            return View("Error");
        }

        public ActionResult ChangePassword()
        {
            return View();
        }

        [Authorize]
        public string GetPossibleFriends()
        {
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", Request.Cookies["access_token"].Value);
                client.BaseAddress = new Uri("http://localhost:24082");

                HttpResponseMessage msg = client.PutAsJsonAsync("/api/Friends/GetPossibleFriends", User.Identity.Name).Result;

                if (msg.IsSuccessStatusCode)
                {
                    List<Tuple<UserInfo, int>> returned = msg.Content.ReadAsAsync<List<Tuple<UserInfo, int>>>().Result;


                    List<Tuple<UserInfo, int, string>> toPass = new List<Tuple<UserInfo, int, string>>();

                    byte[] buf = null;

                    for (int i = 0; i < returned.Count(); ++i)
                    {
                        if (returned[i].Item1.Avatar == null)
                        {
                            if (buf == null)
                            {
                                using (FileStream s = new FileStream(Server.MapPath("~/Common/Images/empty_avatar.jpg"), FileMode.Open, FileAccess.Read))
                                {
                                    buf = new byte[s.Length];
                                    s.Read(buf, 0, (int)s.Length);
                                }
                            }

                            toPass.Add(new Tuple<UserInfo, int, string>(returned[i].Item1, returned[i].Item2, String.Format("data:image/jpg;base64,{0}", Convert.ToBase64String(buf))));
                        }
                        else
                        {
                            toPass.Add(new Tuple<UserInfo, int, string>(returned[i].Item1, returned[i].Item2, String.Format("data:image/jpg;base64,{0}", Convert.ToBase64String(returned[i].Item1.Avatar))));
                        }
                    }

                    return JsonConvert.SerializeObject(toPass);
                }
            }

            return JsonConvert.SerializeObject(new List<Tuple<UserInfo, int, string>>());
        }

        [Authorize]
        public ActionResult DisplayFriends()
        {
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", Request.Cookies["access_token"].Value);
                client.BaseAddress = new Uri("http://localhost:24082");

                HttpResponseMessage msg = client.PostAsJsonAsync("/api/Friends/GetAllUserFriendsInfo", "").Result;

                if (msg.IsSuccessStatusCode)
                {
                    List<UserInfo> users = msg.Content.ReadAsAsync<List<UserInfo>>().Result;
                    byte[] buf = null;

                    foreach (var user in users)
                    {
                        if (user.Avatar == null)
                        {
                            if (buf == null)
                            {
                                using (FileStream s = new FileStream(Server.MapPath("~/Common/Images/empty_avatar.jpg"), FileMode.Open, FileAccess.Read))
                                {
                                    buf = new byte[s.Length];
                                    s.Read(buf, 0, (int)s.Length);
                                }
                            }

                            user.Avatar = buf;
                        }
                    }

                    return View(users);
                }
            }

            return View("Error");
        }

        [Authorize]
        public ActionResult DialogueMessages(string id)
        {
            List<Message> toProcess = new List<Message>();

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", Request.Cookies["access_token"].Value);
                client.BaseAddress = new Uri("http://localhost:24082");

                HttpResponseMessage msg = client.PutAsJsonAsync("/api/Dialogues/GetMessages", id).Result;

                if (msg.IsSuccessStatusCode)
                {
                    toProcess = msg.Content.ReadAsAsync<List<Message>>().Result;
                }
            }

            return PartialView(toProcess);
        }

        [Authorize]
        public ActionResult DialoguePage(string recipientMail)
        {
            if (recipientMail == null) return View("Error");

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", Request.Cookies["access_token"].Value);
                client.BaseAddress = new Uri("http://localhost:24082");

                HttpResponseMessage msg = client.GetAsync("/api/Dialogues/GetDialogue?mail=" + recipientMail).Result;

                if (msg.IsSuccessStatusCode)
                {
                    Dialogue d = msg.Content.ReadAsAsync<Dialogue>().Result;

                    if(d == null)
                    {
                        d = new Dialogue()
                        {
                            Writer1 = new MessageWriter()
                            {
                                Email = User.Identity.Name,
                                LastDeletionTime = DateTime.Now
                            },
                            Writer2 = new MessageWriter()
                            {
                                Email = recipientMail,
                                LastDeletionTime = DateTime.Now
                            },
                            CreationTime = DateTime.Now
                        };

                        msg = client.PostAsJsonAsync("/api/Dialogues/AddDialogue", d).Result;

                        if (!msg.IsSuccessStatusCode)
                        {
                            d = null;
                        }
                    }

                    if (d != null)
                    {
                        return View(d);
                    }
                }
            }

            return View("Error");
        }

        [Authorize]
        [HttpPost]
        public ActionResult PostUserAvatar(HttpPostedFileBase avatar)
        {
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", Request.Cookies["access_token"].Value);
                client.BaseAddress = new Uri("http://localhost:24082");

                byte[] toPass = new byte[avatar.ContentLength];
                avatar.InputStream.Read(toPass, 0, avatar.ContentLength);

                HttpResponseMessage msg = client.PostAsJsonAsync("/api/UserInfo/PostAvatar", toPass).Result;

                if (msg.IsSuccessStatusCode)
                {
                    return RedirectToAction("AccountPage");
                }
            }
            return View("Error");
        }

        [Authorize]
        [HttpPost]
        public ActionResult PostUserProfile(UserInfo info)
        {
            info.TimeZoneDescription = "GMT" + (info.TimeZone >= 0 ? "+" : "") + info.TimeZone + ":00";

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", Request.Cookies["access_token"].Value);
                client.BaseAddress = new Uri("http://localhost:24082");

                HttpResponseMessage msg = client.PostAsJsonAsync("/api/UserInfo/PostProfile", info).Result;

                if (msg.IsSuccessStatusCode)
                {
                    return RedirectToAction("AccountPage");
                }
            }
            return View("Error");
        }

        [HttpPost]
        public ActionResult ChangePassword(string email)
        {
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                client.BaseAddress = new Uri("http://localhost:24082/");

                HttpResponseMessage response = client.GetAsync("api/Account/ChangeUserPassword?email=" + email).Result;
            }


            return RedirectToAction("StartPage");
        }

        public ActionResult SetNewPassword(string request_id)
        {
            ViewData["request_id"] = request_id;

            return View();
        }

        [HttpPost]
        public ActionResult SetNewPassword(string request_id, string password, string confirm_password)
        {
            if (password != confirm_password)
            {
                return RedirectToAction("SetNewPassword", "Main", new { request_id = request_id });
            }

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                client.BaseAddress = new Uri("http://localhost:24082/");

                HttpResponseMessage response = client.PostAsJsonAsync("api/Account/SetNewUserPassword",
                    new Tuple<string, string, string>(request_id, password, confirm_password)
                    ).Result;

                if (response.IsSuccessStatusCode)
                {
                    return RedirectToAction("StartPage");
                }
            }

            return RedirectToAction("SetNewPassword", "Main", new { request_id = request_id });
        }


        [Authorize]
        public ActionResult CreateTarget()
        {
            return View();
        }

        [Authorize]
        [HttpPost]
        public ActionResult CreateTarget(FormCollection collection)
        {
            return View();
        }

        [Authorize]
        public ActionResult FindTargets(TargetSearchOptions options)
        {
            List<DbTarget> toReturn = new List<DbTarget>();

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/sjon"));
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", Request.Cookies["access_token"].Value);
                client.BaseAddress = new Uri("http://localhost:24082");

                HttpResponseMessage msg = client.PostAsJsonAsync("/api/Targets/Search", options).Result;

                if (msg.IsSuccessStatusCode)
                {
                    toReturn = msg.Content.ReadAsAsync<List<DbTarget>>().Result;
                }
            }

            ViewData["elements"] = toReturn;

            return PartialView("_SearchTarget");
        }

        [Authorize]
        public ActionResult TargetPage(string id)
        {
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Request.Cookies["access_token"].Value);
                client.BaseAddress = new Uri("http://localhost:24082");

                HttpResponseMessage msg = client.GetAsync("/api/Targets/Get?facadeId=" + id).Result;

                if (msg.IsSuccessStatusCode)
                {
                    List<DbTarget> targets = new List<DbTarget>();
                    DbTarget currTarget = msg.Content.ReadAsAsync<DbTarget>().Result;
                    while (currTarget != null)
                    {
                        targets.Add(currTarget);
                        currTarget = currTarget.NextTarget;
                    }

                    for (int i = 0; i < targets.Count(); ++i)
                    {
                        targets[i].NextTarget = null;
                    }

                    ViewData["targets"] = targets;
                    ViewData["id"] = id;


                    return View();
                }
            }

            return RedirectToAction("StartPage");
        }

        [Authorize]
        public ActionResult ScheduleConstructor()
        {
            return View();
        }

        [Authorize]
        public async Task<ActionResult> DayTasks(DateTime userTime)
        {

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/sjon"));
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", Request.Cookies["access_token"].Value);
                client.BaseAddress = new Uri("http://localhost:24082");
                HttpResponseMessage msg = await client.PostAsJsonAsync("/api/Schedule/GetSchedule", userTime);

                if(msg.IsSuccessStatusCode)
                {
                    Schedule s = msg.Content.ReadAsAsync<Schedule>().Result;    
                    return PartialView("_DayTasks", s);
                }
            }

            return RedirectToAction("StartPage");
        }

        [Authorize]
        public ActionResult SearchForTargets()
        {
            ViewData["object_id"] = new ObjectId();

            return View();
        }
    }
}
using MongoDB.Bson;
using Scheduler.Models;
using Scheduler.Models.Auxiliary;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Web;
using System.Web.Mvc;
using System.Xml.Serialization;

namespace Scheduler.Controllers
{
    public class MainController : Controller
    {
        // GET: Main
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
            return View();
        }

        public ActionResult ChangePassword()
        {
            return View();
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

                HttpResponseMessage msg = client.GetAsync("/api/Targets/Get?id=" + id).Result;

                List<DbTarget> targets = new List<DbTarget>();
                DbTarget currTarget = msg.Content.ReadAsAsync<DbTarget>().Result;
                while(currTarget != null)
                {
                    targets.Add(currTarget);
                    currTarget = currTarget.NextTarget;
                }

                for(int i = 0; i < targets.Count(); ++i)
                {
                    targets[i].NextTarget = null;
                }

                ViewData["targets"] = targets;
            }

            return View();
        }

        [Authorize]
        public ActionResult SearchForTargets()
        {
            ViewData["object_id"] = new ObjectId();

            return View();
        }
    }
}
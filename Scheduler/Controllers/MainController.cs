using Scheduler.Models;
using Scheduler.Models.Auxiliary;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
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
            //using (FileStream stream = new FileStream(Server.MapPath("~/Common/Text files/start_blocks.xml"), FileMode.OpenOrCreate, FileAccess.Write))
            //{
            //    XmlSerializer ser = new XmlSerializer(typeof(List<StartPageBlockData>));
            //    List<StartPageBlockData> spbd = new List<StartPageBlockData>()
            //{
            //    new StartPageBlockData()
            //    {
            //         Id = "block1",
            //         ColorName = "#bfe9ff",
            //         Title = "Scheduler",
            //         Description = "Pretty good resource for organising your daily activity",
            //         NextBlock = "#block2",
            //         ArrowImageSrc = "../../Common/Images/arrow_down.png"
            //    },
            //    new StartPageBlockData()
            //    {
            //         Id = "block2",
            //         ColorName = "#77A1D3",
            //         Title = "1. Plan your day",
            //         Description = "Pretty good resource for organising your daily activity",
            //         NextBlock = "#block3",
            //         ArrowImageSrc = "../../Common/Images/arrow_down.png"
            //    },
            //    new StartPageBlockData()
            //    {
            //         Id = "block3",
            //         ColorName = "#26a0da",
            //         Title = "2. Set new targets",
            //         Description = "Pretty good resource for organising your daily activity",
            //         NextBlock = "#block4",
            //         ArrowImageSrc = "../../Common/Images/arrow_down.png"
            //    },
            //    new StartPageBlockData()
            //    {
            //         Id = "block4",
            //         ColorName = "#06beb6",
            //         Title = "3. Meet like-minders",
            //         Description = "Pretty good resource for organising your daily activity",
            //         NextBlock = "#block5",
            //         ArrowImageSrc = "../../Common/Images/arrow_down.png"
            //    },
            //    new StartPageBlockData()
            //    {
            //         Id = "block5",
            //         ColorName = "#ffaf7b",
            //         Title = "4. Analyze your productivity",
            //         Description = "Pretty good resource for organising your daily activity",
            //         NextBlock = "#block6",
            //         ArrowImageSrc = "../../Common/Images/arrow_down.png"
            //    },
            //    new StartPageBlockData()
            //    {
            //         Id = "block6",
            //         ColorName = "#ff8235",
            //         Title = "5. Get useful tips",
            //         Description = "Pretty good resource for organising your daily activity",
            //         NextBlock = "#block1",
            //         ArrowImageSrc = "../../Common/Images/arrow_up.png"
            //    }
            //};

            //    ser.Serialize(stream, spbd);
            //}

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

            return RedirectToAction("SetNewPassword", "Main", request_id);
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
    }
}
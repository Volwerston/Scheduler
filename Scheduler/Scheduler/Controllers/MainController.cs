﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Scheduler.Controllers
{
    public class MainController : Controller
    {
        // GET: Main
        public ActionResult StartPage()
        {
            return View();
        }
    }
}
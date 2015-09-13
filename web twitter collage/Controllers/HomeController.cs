using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace web_twitter_collage.Controllers
{
    public class HomeController : Controller
    {
        // GET: Home
        public ActionResult Index()
        {
            return View();
        }

        // POST: Home
        [HttpPost]
        public ActionResult Index(string twitterUserName)
        {
            ViewBag.userName = twitterUserName;

            return View();
        }
    }

    public class TwitterCollage
    {

    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Threading.Tasks;
using System.Configuration;
using LinqToTwitter;
using ImageMagick;

namespace web_twitter_collage.Controllers
{
    public class OAuthController : AsyncController
    {
        // GET: OAuth
        public ActionResult Index()
        {
            return View();
        }

        public async Task<ActionResult> BeginAsync()
        {
            var auth = new MvcAuthorizer
            {
                CredentialStore = new SessionStateCredentialStore
                {
                    ConsumerKey = ConfigurationManager.AppSettings["consumerKey"],
                    ConsumerSecret = ConfigurationManager.AppSettings["consumerSecret"]
                }
            };

            string twitterCallbackUrl = "http://twittercollage.apphb.com/"; //Url.Action("Complete", "OAuth", null, Request.Url.Scheme);
            return await auth.BeginAuthorizationAsync(new Uri(twitterCallbackUrl));
        }

        public async Task<ActionResult> CompleteAsync()
        {
            var auth = new MvcAuthorizer
            {
                CredentialStore = new SessionStateCredentialStore()
            };

            await auth.CompleteAuthorizeAsync(Request.Url);

            return RedirectToAction("Index", "Home");
        }
    }
}
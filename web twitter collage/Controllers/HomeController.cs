using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Configuration;
using System.Threading.Tasks;
using LinqToTwitter;
using web_twitter_collage.Models;
using ImageMagick;



namespace web_twitter_collage.Controllers
{
    public class HomeController : Controller
    {
        // GET: Home
        public ActionResult Index()
        {
            if (!new SessionStateCredentialStore().HasAllCredentials())
                return RedirectToAction("Index", "OAuth");
            
            return View();
        }

        // POST: Home
        [HttpPost]
        [ActionName("Index")]
        public async Task<ActionResult> TweetAsync(LoadUserViewModel user)
        {
            var auth = new MvcAuthorizer
            {
                CredentialStore = new SessionStateCredentialStore()
            };

            var ctx = new TwitterContext(auth);

            Friendship friendship;

            long cursor = -1;

            List<string> urls = new List<string>();
            List<int> counts = new List<int>();
            try
            {
                do
                {
                    friendship =
                        await
                        (from friend in ctx.Friendship
                         where friend.Type == FriendshipType.FriendsList &&
                         friend.ScreenName == user.Text.Trim() &&
                         friend.Cursor == cursor
                         select friend).SingleOrDefaultAsync();

                    if (friendship != null &&
                        friendship.Users != null &&
                        friendship.CursorMovement != null)
                    {
                        cursor = friendship.CursorMovement.Next;

                        friendship.Users.ForEach(friend =>
                        {
                            urls.Add(friend.ProfileImageUrl.Replace("normal","bigger"));
                            counts.Add(friend.StatusesCount);
                        }
                        );
                    }
                } while (cursor != 0);

                //begin generate image collage from urls
                //create image collection
                ViewBag.ImageData = ImageProcessing(urls);

                var responseTweetVM = new LoadUserViewModel
                {
                    Text = user.Text,
                    Response = "Collage loaded"
                };

                return View(responseTweetVM);
            }
            catch (Exception ex)
            {
                ViewBag.errors = ex.Message.Replace("page", "user");
            }
            return View();
        }

        byte[] GenerateCollage(MagickImageCollection collection)
        {
            MontageSettings settings = new MontageSettings();
            settings.BackgroundColor = new MagickColor("#FFF");
            settings.Geometry = new MagickGeometry(73);
            using (MagickImage result = collection.Montage(settings))
            {
                result.Format = MagickFormat.Png;
                return result.ToByteArray();
            }
        }

        string ImageProcessing(List<string> urls)
        {
            byte[] imageByteData = null;
            byte[] data = null;
            using (MagickImageCollection collection = new MagickImageCollection())
            {
                for (int i = 0; i < urls.Count; i++)
                {
                    imageByteData = new System.Net.WebClient().DownloadData(urls[i]);
                    MagickImage image = new MagickImage(imageByteData);
                    collection.Add(image);
                }

                data = GenerateCollage(collection);
            }

            string imageBase64Data = Convert.ToBase64String(data/*imageByteData*/);
            string imageDataURL = string.Format("data:image/png;base64,{0}", imageBase64Data);
            return imageDataURL;
        }
    }
}
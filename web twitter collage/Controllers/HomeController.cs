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
            var TweetVM = new LoadUserViewModel
            {
                Text = "",
                size = 300,
                Response = ""
            };

            return View(TweetVM);
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
            //load profile picture urls and tweets count using linq2twitter
            try
            {
                if (user.size < 24 || user.size > 2000)
                    throw new Exception("Size out of bounds. Min size: 24, Max size: 2000");

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

                //generate image collage from urls
                ViewBag.ImageData = ImageProcessing(urls, user.size);

                //genrate status response
                var responseTweetVM = new LoadUserViewModel
                {
                    Text = user.Text,
                    size = user.size,
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

        //Create single sized collage from image collection
        byte[] GenerateCollage(MagickImageCollection collection, int size)
        {
            MontageMode mode = MontageMode.Concatenate;

            MontageSettings settings = new MontageSettings(mode);
            settings.BackgroundColor = new MagickColor("#FFF");
            settings.Geometry = new MagickGeometry("1x1<");
            using (MagickImage result = collection.Montage(settings))
            {
                result.Format = MagickFormat.Png;
                result.Resize(new MagickGeometry(size));
                return result.ToByteArray();
            }
        }

        //Load images from twitter, process them into collage
        string ImageProcessing(List<string> urls, int size)
        {
            byte[] imageByteData = null;
            byte[] data = null;
            using (MagickImageCollection collection = new MagickImageCollection())
            {

                Random rand = new Random();
                for (int i = 0; i < urls.Count; i++)
                {
                    imageByteData = new System.Net.WebClient().DownloadData(urls[i]);
                    MagickImage tmpImage = new MagickImage(imageByteData);
                    collection.Add(tmpImage);
                }

                data = GenerateCollage(collection, size);
            }

            string imageBase64Data = Convert.ToBase64String(data/*imageByteData*/);
            string imageDataURL = string.Format("data:image/png;base64,{0}", imageBase64Data);
            return imageDataURL;
        }
    }
}
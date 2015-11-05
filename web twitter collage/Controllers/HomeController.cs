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
using System.IO;



namespace web_twitter_collage.Controllers
{
    public class HomeController : Controller
    {
        // GET: Home
        public ActionResult Index()
        {
            if (!new SessionStateCredentialStore().HasAllCredentials())
                return RedirectToAction("Begin", "OAuth");
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
            if (!ModelState.IsValid)
            {
                return View(user);
            }
            //load OAuth credentials
            var auth = new MvcAuthorizer
            {
                CredentialStore = new SessionStateCredentialStore()
            };
            
            var ctx = new TwitterContext(auth);

            Friendship friendship;

            long cursor = -1;

            List<string> urls = new List<string>();
            List<int> counts = new List<int>();
            // load profile picture urls and tweets count using linq2twitter
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

                // generate image collage from urls
                ViewBag.ImageData = ImageProcessing(urls, user.size, counts, user.Resize);

                // genrate status response
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

        // Create single sized collage from image collection
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

        // Load images from twitter, process them into collage
        string ImageProcessing(List<string> urls, int size, List<int> counts, bool resize)
        {
            byte[] imageByteData = null;
            byte[] data = null;
            // Load images into collection
            using (MagickImageCollection collection = new MagickImageCollection())
            {
                
                for (int i = 0; i < urls.Count; i++)
                {
                    imageByteData = new System.Net.WebClient().DownloadData(urls[i]);
                    MagickImage tmpImage = new MagickImage(imageByteData);
                    collection.Add(tmpImage);
                }
                // generade byte array for collage from images collection
                if (resize)
                {
                    // collage with proportional images
                    SizableImages simages = new SizableImages(counts);
                    List<SizableImage> arrangedImages = simages.GetImages();
                    int width = simages.GetXBottom() - simages.GetXTop();
                    int height = simages.GetYBottom() - simages.GetYTop();

                    int maxDimension;

                    if (width < height)
                    {
                        maxDimension = height;
                    }
                    else
                    {
                        maxDimension = width;
                    }

                    double correction = (double)size / maxDimension;


                    MagickReadSettings settings = new MagickReadSettings();
                    settings.Width = (int)(width * correction);
                    settings.Height = (int)(height * correction);
                    using (MagickImage image = new MagickImage("xc:white", settings))
                    {
                        for (int i = 0; i < arrangedImages.Count(); i++)
                        {
                            collection[
                                arrangedImages[i].id
                                ].Resize(new MagickGeometry((int)(arrangedImages[i].size * correction)));
                            image.Composite(collection[arrangedImages[i].id],
                                (int)(arrangedImages[i].positionX * correction),
                                (int)(arrangedImages[i].positionY * correction));
                        }
                        image.Format = MagickFormat.Png;
                        data = image.ToByteArray();
                    }
                }
                else
                {
                    // collage with single sized images
                    data = GenerateCollage(collection, size);
                }
            }
            // convert byte array to data url
            string imageBase64Data = Convert.ToBase64String(data/*imageByteData*/);
            string imageDataURL = string.Format("data:image/png;base64,{0}", imageBase64Data);
            return imageDataURL;
        }
    }

    class SizableImage
    {
        public int id;
        public int size;
        public int positionX;
        public int positionY;
    }

    class SizableImages
    {
        List<SizableImage> images = new List<SizableImage>();
        int tweetsTotalCount = 0;
        int MAX_SIZE = 300;
        int x_top = 0;
        int y_top = 0;
        int x_bottom = 0;
        int y_bottom = 0;
        bool arranged = false;

        public SizableImages(List<int> counts)
        {
            for (int i = 0; i < counts.Count(); i++)
            {
                tweetsTotalCount = counts.Sum();
                SizableImage simage = new SizableImage();
                simage.id = i;
                simage.size = 1 + (int)(MAX_SIZE * (double)counts[i] / tweetsTotalCount);
                simage.positionX = 0;
                simage.positionY = 0;
                images.Add(simage);
            }
            images.Sort((x, y) => (-x.size).CompareTo(-y.size));
        }

        void ArrangeImages()
        {
            arranged = true;
            
            x_bottom = images[0].size;
            y_bottom = images[0].size;
            int box_x_top = 0;
            int box_y_top = 0;
            int box_x_bottom = 0;
            int box_y_bottom = 0;
            bool vertical = true;
            for (int i = 1; i < images.Count(); i++)
            {
                if (images[i].size < box_x_bottom - box_x_top &&
                    images[i].size < box_y_bottom - box_y_top)
                {
                    images[i].positionX = box_x_top;
                    images[i].positionY = box_y_top;
                    if (vertical)
                        box_y_top += images[i].size;
                    else
                        box_x_top += images[i].size;
                }
                else if (x_bottom - x_top <= y_bottom - y_top)
                {
                    box_x_top = x_bottom;
                    box_y_top = y_top;
                    box_x_bottom = x_bottom + images[i].size;
                    box_y_bottom = y_bottom;
                    x_bottom = box_x_bottom;
                    y_bottom = box_y_bottom;
                    images[i].positionX = box_x_top;
                    images[i].positionY = box_y_top;
                    box_y_top += images[i].size;
                    vertical = true;
                }
                else
                {
                    box_x_top = x_top;
                    box_y_top = y_bottom;
                    box_x_bottom = x_bottom;
                    box_y_bottom = y_bottom + images[i].size;
                    x_bottom = box_x_bottom;
                    y_bottom = box_y_bottom;
                    images[i].positionX = box_x_top;
                    images[i].positionY = box_y_top;
                    box_x_top += images[i].size;
                    vertical = false;
                }
            }
        }

        public List<SizableImage> GetImages()
        {
            if (!arranged)
                ArrangeImages();
            return images;
        }

        public int GetXTop()
        {
            if (!arranged)
                ArrangeImages();
            return x_top;
        }

        public int GetYTop()
        {
            if (!arranged)
                ArrangeImages();
            return y_top;
        }

        public int GetXBottom()
        {
            if (!arranged)
                ArrangeImages();
            return x_bottom;
        }

        public int GetYBottom()
        {
            if (!arranged)
                ArrangeImages();
            return y_bottom;
        }
    }
}
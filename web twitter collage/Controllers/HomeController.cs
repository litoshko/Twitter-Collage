using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Configuration;
using System.Threading.Tasks;
using LinqToTwitter;
using web_twitter_collage.Models;

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
                         friend.ScreenName == user.Text &&
                         friend.Cursor == cursor
                         select friend).SingleOrDefaultAsync();

                    if (friendship != null &&
                        friendship.Users != null &&
                        friendship.CursorMovement != null)
                    {
                        cursor = friendship.CursorMovement.Next;

                        friendship.Users.ForEach(friend =>
                        {
                            urls.Add(friend.ProfileImageUrl);
                            counts.Add(friend.StatusesCount);
                        }
                        );
                    }
                } while (cursor != 0);

                var responseTweetVM = new LoadUserViewModel
                {
                    Text = user.Text,
                    Response = urls[0] + counts[0].ToString()
                };

                return View(responseTweetVM);
            }
            catch (Exception ex)
            {
                ViewBag.errors = ex.Message.Replace("page", "user");
            }
            return View();
        }
    }

    public class TwitterCollage
    {
        static string username = "";
        static List<string> urls = new List<string>();
        static List<int> counts = new List<int>();
        static string errors = "";

        public static void LoadUserInfo( string inUserName )
        {
            username = inUserName;

            Task loadTask = RunUser();
            loadTask.Wait();
            //try
            //{
            //    Task loadTask = RunUser();
            //    loadTask.Wait();
            //}
            //catch (Exception ex)
            //{
            //    errors = ex.ToString();
            //}
        }

        private static async Task RunUser()
        {
            var auth = new MvcAuthorizer
            {
                CredentialStore = new SessionStateCredentialStore()
            };

            var ctx = new TwitterContext(auth);

            Friendship friendship;

            long cursor = -1;
            do
            {
                friendship =
                    await
                    (from friend in ctx.Friendship
                     where friend.Type == FriendshipType.FriendsList &&
                     friend.ScreenName == username &&
                     friend.Cursor == cursor
                     select friend)
                     .SingleOrDefaultAsync();

                if (friendship != null &&
                    friendship.Users != null &&
                    friendship.CursorMovement != null)
                {
                    cursor = friendship.CursorMovement.Next;

                    friendship.Users.ForEach(friend =>
                    {
                        urls.Add(friend.ProfileImageUrl);
                        counts.Add(friend.StatusesCount);
                    }
                    );

                }
            } while (cursor != 0);

            //await ShowUserDetailsAsync(ctx);

        }

        private static async Task ShowUserDetailsAsync(TwitterContext twitterCtx)
        {
            // TODO: add protection against rate limits
            Friendship friendship;
            
            long cursor = -1;
            do
            {
                friendship =
                    await
                    (from friend in twitterCtx.Friendship
                     where friend.Type == FriendshipType.FriendsList &&
                     friend.ScreenName == username &&
                     friend.Cursor == cursor
                     select friend)
                     .SingleOrDefaultAsync();

                if (friendship != null &&
                    friendship.Users != null &&
                    friendship.CursorMovement != null)
                {
                    cursor = friendship.CursorMovement.Next;

                    friendship.Users.ForEach(friend =>
                    {
                        urls.Add(friend.ProfileImageUrl);
                        counts.Add(friend.StatusesCount);
                    }
                    );

                }
            } while (cursor != 0);
        }

        public static string Errors()
        {
            return errors;
        }
    }
}
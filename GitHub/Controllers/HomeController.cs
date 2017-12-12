using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Octokit;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Hangfire;

namespace WebApplication1.Controllers
{
    public class HomeController : Controller
    {
        //Not actual client details, nice try! - moved to json file for production
        string clientId = "92580a792a54fd85c43a";
        string clientSecret = "";
        //Create an instance of the Octokit client
        GitHubClient client = new GitHubClient(new ProductHeaderValue("gitterbug"));

        public IActionResult Index()
        {
            ViewData["login"] = "/Home/Login";
            //Test HangFire job that is enqueued - appears in local dashboard
            BackgroundJob.Enqueue(() => Console.Write("hey"));
            return View();
        }

        //Used in OAuth flow to retrieve code in redirect
        public async Task<ActionResult> Authorize(string code)
        {
            //If the code is not retrieved
            if (String.IsNullOrEmpty(code))
                return RedirectToAction("Index");

            //Request Access Token
            var request = new OauthTokenRequest(clientId, clientSecret, code);
            var token = await client.Oauth.CreateAccessToken(request);
            var x = token.AccessToken;
            HttpContext.Session.SetString("token", x);
            client.Credentials = new Credentials(x);
            return RedirectToAction("About");
        }

        public IActionResult SignOut()
        {
            //Clear the session thus removing reference to access token
            HttpContext.Session.Clear();
            return RedirectToAction("Index");
        }

        public PartialViewResult LoginLayout()
        {
            ViewBag.authenticated = false;
            if (!String.IsNullOrEmpty(HttpContext.Session.GetString("token")))
                ViewBag.authenticated = true;
            return PartialView("~/Views/Shared/LoginLayout.cshtml");
        }

        public IActionResult Login()
        {
            //Begin the OAuth flow, request user and notifications permissions for now
            var request = new OauthLoginRequest(clientId)
            {
                Scopes = { "user", "notifications" }
            };

            var oauthLoginUrl = client.Oauth.GetGitHubLoginUrl(request);
            return Redirect(oauthLoginUrl.ToString());
        }


        public async Task<IActionResult> About()
        {
            try
            {
                string code = HttpContext.Session.GetString("token");
                if (String.IsNullOrEmpty(code))
                    return RedirectToAction("Error");
                Dictionary<string, int> languages = new Dictionary<string, int>();
                client.Credentials = new Credentials(code);
                //Retrieve user details asynchronously
                var user = await client.User.Current();
                //Retrieve current user repos (including private) async
                var repositories = await client.Repository.GetAllForCurrent();
                List<Octokit.Repository> repos = new List<Octokit.Repository>();
                foreach (var y in repositories)
                {
                    repos.Add(y);
                    if (languages.ContainsKey(y.Language))
                        languages[y.Language] = languages[y.Language] + 1;
                    else
                        languages.Add(y.Language, 0);
                }
                //Pass details to frontend through viewbag to be rendered by Razer
                ViewBag.username = user.Name;
                ViewBag.icon = user.AvatarUrl;
                ViewBag.repos = repos;
                ViewBag.languages = languages;
                return View(user);
            }
            catch (AuthorizationException)
            {
                //If we fail to auth, redirect to unauthorized page
                return RedirectToAction("Error");
            }
        }

        public IActionResult Error()
        {
            return View();
        }

        public IActionResult Trending()
        {
            return View();
        }
    }
}

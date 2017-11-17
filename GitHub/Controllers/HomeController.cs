using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Octokit;
using Microsoft.AspNetCore.Http;

namespace WebApplication1.Controllers
{
    public class HomeController : Controller
    {
        string clientId = "client-id";
        string clientSecret = "client-secret";
        GitHubClient client = new GitHubClient(new ProductHeaderValue("gitterbug"));

        public IActionResult Index()
        {
            ViewData["login"] = "/Home/Login";
            return View();
        }

        public async Task<ActionResult> Authorize(string code)
        {
            if (String.IsNullOrEmpty(code))
                return RedirectToAction("Index");

            var request = new OauthTokenRequest(clientId, clientSecret, code);
            var token = await client.Oauth.CreateAccessToken(request);
            var x = token.AccessToken;
            HttpContext.Session.SetString("token", x);
            client.Credentials = new Credentials(x);
            return RedirectToAction("About");
        }

        public IActionResult SignOut()
        {
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

                client.Credentials = new Credentials(code);
                var user = await client.User.Current();
                var repositories = await client.Repository.GetAllForCurrent();
                List<string> repos = new List<string>();
                foreach (var y in repositories)
                    repos.Add(y.Name);
                ViewBag.username = user.Name;
                ViewBag.icon = user.AvatarUrl;
                ViewBag.repos = repos;
                return View(user);
            }
            catch (AuthorizationException)
            {
                return RedirectToAction("Error");
            }
        }

        public IActionResult Error()
        {
            return View();
        }
    }
}

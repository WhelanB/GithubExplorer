﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Octokit;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Hangfire;
using Newtonsoft.Json;
using System.Dynamic;
using GitHub.Controllers;
using System.Net.Http;

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
            string code = HttpContext.Session.GetString("token");
            if (!String.IsNullOrEmpty(code))
                return View("Followers");
            return View();
        }

        public async Task<IActionResult> Graph()
        {
            string code = HttpContext.Session.GetString("token");
            client.Credentials = new Credentials(code);
            var repositories = await client.Repository.GetAllForCurrent();
            var user = await client.User.Current();
            Graph json = new Graph(user.Login, user.AvatarUrl, "/Home/Profile");
            Dictionary<string, Graph> userLanguages = new Dictionary<string, Graph>();
            foreach (Repository repo in repositories) {
                if (repo.Language == null)
                    break;
                if (!userLanguages.ContainsKey(repo.Language))
                    userLanguages.Add(repo.Language, null);
                userLanguages[repo.Language] = new Graph(repo.Language, "https://dummyimage.com/64x64/000/fff&text=" + repo.Language, "javascript:void(0);");
            }
            var followersJSON = await GetAsync("https://api.github.com/users/"+ user.Login +"/followers");
            
            List<dynamic> followers = JsonConvert.DeserializeObject<List<dynamic>>(followersJSON);
            foreach (dynamic f in followers)
            {
                var repos = await client.Repository.GetAllForUser((string)f.login);
                foreach(Repository r in repos)
                {
                    if(r.Language != null && userLanguages.ContainsKey(r.Language))
                        userLanguages[r.Language].AddChild(new Graph((string)f.login, (string)f.avatar_url, "/Home/Profile?username=" + (string)f.login));
                }
            }

            foreach(Graph g in userLanguages.Values)
            {
                json.AddChild(g);
            }
            var response = JsonConvert.SerializeObject(json, Formatting.Indented,
                            new JsonSerializerSettings
                            {
                                NullValueHandling = NullValueHandling.Ignore
                            });
            return Content(response);
        }

        public async Task<string> GetAsync(string uri)
        {
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Authorization", "token " + HttpContext.Session.GetString("token"));
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Gitterbug");
            var response = await httpClient.GetAsync(uri);

            //response.EnsureSuccessStatusCode();

            string content = await response.Content.ReadAsStringAsync();
            return content;
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
            return RedirectToAction("Profile");
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


        public async Task<IActionResult> Profile(string username)
        {
            try
            {
                string code = HttpContext.Session.GetString("token");
                if (String.IsNullOrEmpty(code))
                    return RedirectToAction("Error");
                Dictionary<string, int> languages = new Dictionary<string, int>();
                client.Credentials = new Credentials(code);
                //Retrieve user details asynchronously
                User user;
                IReadOnlyList<Repository> repositories;
                if (String.IsNullOrEmpty(username))
                {
                    user = await client.User.Current();
                    repositories = await client.Repository.GetAllForCurrent();
                }
                else
                {
                    user = await client.User.Get(username);
                    repositories = await client.Repository.GetAllForUser(username);
                }
                //Retrieve current user repos (including private) async

                
                List<Octokit.Repository> repos = new List<Octokit.Repository>();
                foreach (var y in repositories)
                {
                    repos.Add(y);
                    string language = y.Language;
                    if (language == null)
                        language = "Undefined";
                    if (languages.ContainsKey(language))
                        languages[language] = languages[language] + 1;
                    else
                        languages.Add(language, 1);
                }
                //Pass details to frontend through viewbag to be rendered by Razer
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


    }
}

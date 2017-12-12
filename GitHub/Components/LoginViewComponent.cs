using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Octokit;
using System;
using System.Threading.Tasks;

public class LoginViewComponent : ViewComponent
{

    public LoginViewComponent()
    {

    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        string accesstoken = HttpContext.Session.GetString("token");
        if (!String.IsNullOrEmpty(accesstoken))
        {
            //If we are authenticated, return the user which will render "SignOut"
            GitHubClient client = new GitHubClient(new ProductHeaderValue("gitterbug"));
            client.Credentials = new Credentials(accesstoken);
            var user = await client.User.Current();
            return View(user);
        }
        else
        {
            //Otherwise pass false through to frontend to render Login
            ViewBag.authenticated = false;
            return View(null);
        }
    }
}
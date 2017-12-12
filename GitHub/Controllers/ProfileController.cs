using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;

namespace GitHub.Controllers
{
    public class ProfileController : Controller
    {
        private IHostingEnvironment _env;

        public ProfileController(IHostingEnvironment env)
        {
            _env = env;
       

        }


        public IActionResult Index()
        {
            var webRoot = _env.WebRootPath;
            var file = System.IO.Path.Combine(webRoot, "Home/data/trends.json");
            string json = System.IO.File.ReadAllText(file);
            return Content(json);
        }
    }
}
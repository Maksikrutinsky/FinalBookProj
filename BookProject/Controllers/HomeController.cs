using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using BookProject.Models;

namespace BookProject.Controllers
{
    public class HomeController : Controller
    {
        EBookLibraryEntities db = new EBookLibraryEntities();
    
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Contact()
        {
            return View();
        }
        
        public ActionResult About()
        {
            return View();
        }
        
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace COMP2084GetMeAGame.Controllers
{
    public class DummiesController : Controller
    {
        // constructor method so an instance of this class be be created
        public DummiesController()
        {
        }

        public IActionResult Index()
        {
            return View();
        }
    }
}

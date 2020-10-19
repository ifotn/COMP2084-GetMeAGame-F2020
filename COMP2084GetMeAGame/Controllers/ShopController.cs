using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using COMP2084GetMeAGame.Data;
using Microsoft.AspNetCore.Mvc;

namespace COMP2084GetMeAGame.Controllers
{
    public class ShopController : Controller
    {
        // db connection
        private readonly ApplicationDbContext _context;

        // connect to the db whenever this controller is used
        public ShopController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            // get list of categories to display to customers on the main shopping page
            var categories = _context.Categories.OrderBy(c => c.Name).ToList();
            return View(categories);
        }
    }
}

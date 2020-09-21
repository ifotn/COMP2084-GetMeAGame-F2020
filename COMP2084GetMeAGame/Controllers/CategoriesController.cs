using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using COMP2084GetMeAGame.Models;
using Microsoft.AspNetCore.Mvc;

namespace COMP2084GetMeAGame.Controllers
{
    public class CategoriesController : Controller
    {
        public IActionResult Index()
        {
            // use our category model & some fake data to pass to the view
            // make an empty list of categories
            var categories = new List<Category>();

            // use a loop to make 10 categories and add each one to the list
            for (var i = 1; i <= 10; i++)
            {
                categories.Add(new Category { Id = i, Name = "Category " + i.ToString() });
            }
            
            // now pass the categories list when loading the view
            return View(categories);
        }

        public IActionResult Browse(string categoryName)
        {
            // take the category name passed in with the link and store it in the viewbag for display
            ViewBag.categoryName = categoryName;

            // load the view /Views/Categories/Browse
            return View();
        }
    }
}

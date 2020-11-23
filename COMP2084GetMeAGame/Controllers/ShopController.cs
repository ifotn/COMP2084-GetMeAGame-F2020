﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using COMP2084GetMeAGame.Data;
using COMP2084GetMeAGame.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

// add references for Stripe
using Stripe;
using System.Configuration;
using Stripe.Checkout;

namespace COMP2084GetMeAGame.Controllers
{
    public class ShopController : Controller
    {
        // db connection
        private readonly ApplicationDbContext _context;

        // configuration dependency needed to read Stripe Keys from appsettings.json or the secret key store
        private IConfiguration _configuration;

        // connect to the db whenever this controller is used
        // this controller uses Depedency Injection - it requires a db connection object when it's created
        public ShopController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public IActionResult Index()
        {
            // get list of categories to display to customers on the main shopping page
            var categories = _context.Categories.OrderBy(c => c.Name).ToList();
            return View(categories);
        }

        // Shop/Browse/3
        public IActionResult Browse(int id)
        {
            // get the products in the selected category
            var products = _context.Products.Where(p => p.CategoryId == id).OrderBy(p => p.Name).ToList();

            // load the Browse page and pass it the list of products to display
            return View(products);
        }

        // GET: /Shop/AddToCart
        [HttpPost]
        public IActionResult AddToCart(int ProductId, int Quantity)
        {
            // get current price of the product
            var price = _context.Products.Find(ProductId).Price;

            // identify the customer
            var customerId = GetCustomerId();

            // check if product already exists in this user's cart
            var cartItem = _context.Carts.SingleOrDefault(c => c.ProductId == ProductId && c.CustomerId == customerId);

            if (cartItem != null)
            {
                // product already exists so update the quantity
                cartItem.Quantity += Quantity;
                _context.Update(cartItem);
                _context.SaveChanges();
            }
            else
            {
                // create a new Cart object
                var cart = new Cart
                {
                    ProductId = ProductId,
                    Quantity = Quantity,
                    Price = price,
                    CustomerId = customerId,
                    DateCreated = DateTime.Now
                };

                // use the Carts DbSet in ApplicationContext.cs to save to the database
                _context.Carts.Add(cart);
                _context.SaveChanges();
            }
                
            // redirect to show the current cart
            return RedirectToAction("Cart");
        }

        private string GetCustomerId()
        {
            // is there already a session variable holding an identifier for this customer?
            if (HttpContext.Session.GetString("CustomerId") == null)
            {
                // cart is empty, user is unknown
                var customerId = "";

                // use a Guid to generate a new unique identifier
                customerId = Guid.NewGuid().ToString();

                // now store the new identifier in a session variable
                HttpContext.Session.SetString("CustomerId", customerId);
            }

            // return the CustomerId to the AddToCart method
            return HttpContext.Session.GetString("CustomerId");
        }
        
        // GET: /Shop/Cart
        public IActionResult Cart()
        {
            // get CustomerId from the session variable
            var customerId = HttpContext.Session.GetString("CustomerId");

            // get items in this customer's cart - add reference to the parent object: Product
            var cartItems = _context.Carts.Include(c => c.Product).Where(c => c.CustomerId == customerId).ToList();

            // count the # of items in the Cart and write to a session variable to display in the navbar
            var itemCount = (from c in _context.Carts
                             where c.CustomerId == customerId
                             select c.Quantity).Sum();
            HttpContext.Session.SetInt32("ItemCount", itemCount);

            // load the cart page and display the customer's items
            return View(cartItems);
        }

        // GET: /Shop/RemoveFromCart/12
        public IActionResult RemoveFromCart(int id)
        {
            // remove the selected item from Carts table
            var cartItem = _context.Carts.Find(id);

            if (cartItem != null)
            {
                _context.Carts.Remove(cartItem);
                _context.SaveChanges();
            }

            // redirect to updated Cart page
            return RedirectToAction("Cart");
        }

        // GET: /Shop/Checkout
        [Authorize]
        public IActionResult Checkout()
        {
            return View();
        }

        // POST: /Shop/Checkout
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Checkout([Bind("Address,City,Province,PostalCode")] Models.Order order)
        {
            // auto-fill the 3 properties we removed from the form
            order.OrderDate = DateTime.Now;
            order.CustomerId = User.Identity.Name;
            order.Total = (from c in _context.Carts
                           where c.CustomerId == HttpContext.Session.GetString("CustomerId")
                           select c.Quantity * c.Price).Sum();

            // now store Order in a session variable before moving to Payment
            HttpContext.Session.SetObject("Order", order);

            // load the payment page
            return RedirectToAction("Payment");
        }

        // GET: /Shop/Payment
        [Authorize]
        public IActionResult Payment()
        {
            // get the order from the Session variable
            var order = HttpContext.Session.GetObject<Models.Order>("Order");

            // fetch & display the Order Total to the customer
            ViewBag.Total = order.Total;

            // also use the ViewBag to set the PublishableKey, which we can read from the Configuration
            ViewBag.PublishableKey = _configuration.GetSection("Stripe")["PublishableKey"];

            // load the Payment view
            return View();
        }

        // POST: /Shop/Payment
        [Authorize]
        [HttpPost]
        public ActionResult ProcessPayment()
        {
            // get order from session variable
            var order = HttpContext.Session.GetObject<Models.Order>("Order");

            // get Stripe Secret Key from the configuration
            StripeConfiguration.ApiKey = _configuration.GetSection("Stripe")["SecretKey"];

            // .net integration code from https://stripe.com/docs/checkout/integration-builder
            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string>
                {
                  "card",
                },
                LineItems = new List<SessionLineItemOptions>
                {
                  new SessionLineItemOptions
                  {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                      UnitAmount = (long?)(order.Total * 100),
                      Currency = "cad",
                      ProductData = new SessionLineItemPriceDataProductDataOptions
                      {
                        Name = "COMP2084 Get Me a Game Purchase",
                      },
                    },
                    Quantity = 1,
                  },
                },
                Mode = "payment",
                SuccessUrl = "https://" + Request.Host + "/Shop/SaveOrder",
                CancelUrl = "https://" + Request.Host + "/Shop/Cart"
            };

            var service = new SessionService();
            Session session = service.Create(options);
            return Json(new { id = session.Id });
        }

        // GET: /Shop/SaveOrder
        [Authorize]
        public IActionResult SaveOrder()
        {
            // the current order from session variable
            var order = HttpContext.Session.GetObject<Models.Order>("Order");

            // create a new order in the db, this generates and copies the new Id to this order object
            _context.Orders.Add(order);
            _context.SaveChanges();

            // copy each item from the user's cart to a new OrderDetail record
            var cartItems = _context.Carts.Where(c => c.CustomerId == HttpContext.Session.GetString("CustomerId"));

            foreach (var item in cartItems)
            {
                var orderDetail = new OrderDetail
                {
                    OrderId = order.Id,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    Cost = item.Price
                };

                _context.OrderDetails.Add(orderDetail);
            }

            // save new line items to db
            _context.SaveChanges();

            // empty the cart
            foreach (var item in cartItems)
            {
                _context.Carts.Remove(item);
            }

            _context.SaveChanges();

            // load the Details page for the new order 
            return RedirectToAction("Details", "Orders", new { @id = order.Id });
        }
    }
}

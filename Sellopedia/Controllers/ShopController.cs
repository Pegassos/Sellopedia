﻿using Microsoft.AspNet.Identity;
using Newtonsoft.Json;
using Sellopedia.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Sellopedia.Controllers
{
    public class ShopController : Controller
    {
        ApplicationDbContext db = new ApplicationDbContext();
        private ApplicationUser user = null;

        public ActionResult Index(string search, int? categoryId)
        {
            // ! should show a message of no result found in the view

            if(categoryId != null)
            {
                var productsSearch = db.Products
                    .Where(p => p.CategoryId == categoryId)
                    .ToList();
                return View(productsSearch);
            }

            if (search != null)
            {
                var productsSearch = db.Products.Where(p => p.Name.Contains(search)).ToList();
                return View(productsSearch);
            }

            var products = db.Products.ToList();
            return View(products);
        }

        public ActionResult Product(int? id)
        {
            if (id == null || db.Products.Find(id) == null)
            {
                return RedirectToAction("Index");
            }

            Product product = db.Products.Find(id);

            ViewBag.Categories = db.Categories.ToList();
            ViewBag.User = db.Users.Find(User.Identity.GetUserId());
            ViewBag.ProductImage = db.ProductImages.ToList();

            // get the mean score of all the reviews of this product
            int reviewsCount = product.Reviews.ToList().Count,
                reviewsScoreTotal = product.Reviews.Select(r => r.Score).Sum();

            ViewBag.ProductRating = (float)reviewsScoreTotal / reviewsCount;

            return View(product);
        }

        [Authorize]
        public ActionResult Cart()
        {
            return View();
        }

        [Authorize]
        public ActionResult Checkout()
        {
            ApplicationUser user = db.Users.Find(User.Identity.GetUserId());

            return View(user);
        }

        [Authorize]
        [HttpPost]
        public ActionResult CreateReview(Review model)
        {
            // check if user already posted a review about his product
            int reviewCount = db.Reviews.Where(r => r.UserId == model.UserId && r.ProductId == model.ProductId).ToList().Count;


            if(!ModelState.IsValid || reviewCount > 0)
            {
                if(reviewCount > 0)
                {
                    ViewBag.reviewError = "You already have a review on this product. Only one allowed !";
                }
                return RedirectToAction("Product", new { id = model.ProductId });
            }

            db.Reviews.Add(model);
            db.SaveChanges();

            return Json(new { userId = model.UserId, productId = model.ProductId, score = model.Score, message = model.Message }, JsonRequestBehavior.AllowGet);
        }





        //------------ Functional Methods ------------//
        public JsonResult AddToCart(int product_id)
        {
            db.Configuration.ProxyCreationEnabled = false;

            Product product = db.Products.Find(product_id);
            string product_main_image = db.ProductImages.Where(image => image.ProductId == product_id).FirstOrDefault().Image;

            //Order order = new Order()
            //{
            //    UserId = User.Identity.GetUserId(),
            //    ProductId = product.Id,
            //    OrderPrice = (decimal) (product.DiscountPrice != null ? product.DiscountPrice : product.OriginalPrice),
            //    Quantity = 1
            //};

            var order = new
            {
                UserId = User.Identity.GetUserId(),
                OrderPrice = (decimal)(product.DiscountPrice != null ? product.DiscountPrice : product.OriginalPrice),
                CurrentProduct = new
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    ProductImage = product_main_image,
                    ProductPrice = (decimal)(product.DiscountPrice != null ? product.DiscountPrice : product.OriginalPrice),
                },
                Quantity = 1
            };

            return Json(order, JsonRequestBehavior.AllowGet);
        }

        public JsonResult SearchProduct(string search_text)
        {
            var result = db.Products.Where(p => p.Name.Contains(search_text))
                .Select(p => p.Name)
                .ToList();

            return Json(result, JsonRequestBehavior.AllowGet);
        }
    }
}
using System;
using Microsoft.AspNetCore.Mvc;
using BankAccounts.Models;
using System.Linq;
using Microsoft.AspNetCore.Identity;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace BankAccounts.Controllers
{
    public class HomeController : Controller
    {
        private MyContext dbContext;

        public HomeController(MyContext context)
        {
            dbContext = context;
        }
        
        [HttpGet("")]
        
        public ViewResult Index()
        {

            return View();
        }
        [HttpPost("register")]
        public IActionResult RegisterUser(RegisterUser regFromForm)
        {

            if(ModelState.IsValid)
            {
                PasswordHasher<RegisterUser> Hasher = new PasswordHasher<RegisterUser>();
                regFromForm.Password = Hasher.HashPassword(regFromForm, regFromForm.Password);

                if(dbContext.Users.Any(u => u.Email == regFromForm.Email))
                {
                    ModelState.AddModelError("Email", "Email already in use!");
                    return View("Index");
                }
                else{
                    dbContext.Add(regFromForm);
                    dbContext.SaveChanges();
                    HttpContext.Session.SetObjectAsJson("LoggedInUser", regFromForm);
                    return RedirectToAction("Success");
                }
            }

            return View("Index");

        }
        [HttpPost("login")]
        public IActionResult LoginUser(LoginUser LoginFromForm)
        {
           if(ModelState.IsValid)
            {
                // If inital ModelState is valid, query for a user with provided email
                var userInDb = dbContext.Users.FirstOrDefault(u => u.Email == LoginFromForm.LoginEmail);
                // If no user exists with provided email
                if(userInDb == null)
                {
                    // Add an error to ModelState and return to View!
                    ModelState.AddModelError("LoginEmail", "Invalid Email or Password");
                    return View("Index");
                }
                
                // Initialize hasher object
                var hasher = new PasswordHasher<LoginUser>();
                
                // verify provided password against hash stored in db
                var result = hasher.VerifyHashedPassword(LoginFromForm, userInDb.Password, LoginFromForm.LoginPassword);
                
                // result can be compared to 0 for failure
                if(result == 0)
                {
                    ModelState.AddModelError("LoginEmail", "Invalid Email or Password");
                    return View("Index");
                } 
                else
                {
                    HttpContext.Session.SetObjectAsJson("LoggedInUser", userInDb);
                    return RedirectToAction("Success");
                }
                
            }

            return View("Index");
        }


        [HttpGet("success")]

        public IActionResult Success()
        {
            RegisterUser fromLogin = HttpContext.Session.GetObjectFromJson<RegisterUser>("LoggedInUser");
            if(fromLogin == null)
            {
                return RedirectToAction("Index");
            }

            ViewBag.FirstName = fromLogin.FirstName;
            ViewBag.LastName = fromLogin.LastName;
            IndexWrapper IndexWrapper = new IndexWrapper();
            List<Transaction> AllTransactions = dbContext.Transactions
                .Where(r => r.UserId == fromLogin.UserId.ToString())
                .ToList();
                
            IndexWrapper.Transactions = Enumerable.Reverse(
                dbContext.Transactions
                .Where(r => r.UserId == fromLogin.UserId.ToString())
                .OrderBy(t => t.CreatedAt))
                .ToList();

            decimal balance = (decimal)AllTransactions.Sum(t =>t.Amount);
            string balanceString = balance.ToString("###,###,###,##0.00");
            HttpContext.Session.SetString("Balance", balanceString);
            ViewBag.Balance = HttpContext.Session.GetString("Balance");

            return View("Success",IndexWrapper);
        }
        [HttpGet("logout")]

        public ViewResult Logout()
        {
            HttpContext.Session.Clear();
            return View("Index");
        }

        [HttpPost("newtransaction")]

        public IActionResult NewTransaction(Transaction fromForm)
        {
            RegisterUser fromLogin = HttpContext.Session.GetObjectFromJson<RegisterUser>("LoggedInUser");
            if(fromLogin == null)
            {
                return RedirectToAction("Index");
            }
            string currentBalance = HttpContext.Session.GetString("Balance");
            decimal decCurrentBalance = Convert.ToDecimal(currentBalance);
            if(fromForm.Amount<0 && (decCurrentBalance + fromForm.Amount) < 0)
            {
                ModelState.AddModelError("Amount","You do not have that much money to withdrawl.");
            }
            else
            {
                Transaction newT = new Transaction();
                newT.Amount = fromForm.Amount;
                newT.UserId = fromLogin.UserId.ToString();
                dbContext.Transactions.Add(newT);
                dbContext.SaveChanges();
            }


            ViewBag.FirstName = fromLogin.FirstName;
            ViewBag.LastName = fromLogin.LastName;
            IndexWrapper IndexWrapper = new IndexWrapper();
            List<Transaction> AllTransactions = dbContext.Transactions
                .Where(r => r.UserId == fromLogin.UserId.ToString())
                .ToList();
            IndexWrapper.Transactions = Enumerable.Reverse(
                dbContext.Transactions
                .Where(r => r.UserId == fromLogin.UserId.ToString())
                .OrderBy(t => t.CreatedAt))
                .ToList();



            decimal balance = (decimal)AllTransactions.Sum(t =>t.Amount);
            string balanceString = balance.ToString("###,###,###,##0.00");
            HttpContext.Session.SetString("Balance", balanceString);
            ViewBag.Balance = HttpContext.Session.GetString("Balance");
            return View("Success",IndexWrapper);

        }

    }
    public static class SessionExtensions
    {
        // We can call ".SetObjectAsJson" just like our other session set methods, by passing a key and a value
        public static void SetObjectAsJson(this ISession session, string key, object value)
        {
            // This helper function simply serializes theobject to JSON and stores it as a string in session
            session.SetString(key, JsonConvert.SerializeObject(value));
        }
        
        // generic type T is a stand-in indicating that we need to specify the type on retrieval
        public static T GetObjectFromJson<T>(this ISession session, string key)
        {
            string value = session.GetString(key);
            // Upon retrieval the object is deserialized based on the type we specified
            return value == null ? default(T) : JsonConvert.DeserializeObject<T>(value);
        }
    }
}
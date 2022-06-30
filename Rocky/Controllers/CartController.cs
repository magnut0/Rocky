using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Rocky_DataAccess;
using Rocky_Models;
using Rocky_Models.ViewModels;
using Rocky_Utility;
using System.Security.Claims;
using System.Text;

namespace Rocky.Controllers
{
    [Authorize]
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IEmailSender _emailSender;
        [BindProperty] // по умолчанию для POST запросов
        public ProductUserVM productUserVM { get; set; }

        public CartController(ApplicationDbContext db, IWebHostEnvironment webHostEnvironment, IEmailSender emailSender)
        {
            _db = db;
            _webHostEnvironment = webHostEnvironment;
            _emailSender = emailSender;
        }

        public IActionResult Index()
        {
            List<ShoppingCart> shoppingCartList = new List<ShoppingCart>();
            if(HttpContext.Session.Get<IEnumerable<ShoppingCart>>(WC.SessionCard) != null && 
                HttpContext.Session.Get<IEnumerable<ShoppingCart>>(WC.SessionCard).Count() > 0)
            {
                // session exests
                shoppingCartList = HttpContext.Session.Get<IEnumerable<ShoppingCart>>(WC.SessionCard).ToList();
            }
            // Select - какие столбцы выбрать
            List<int> prodInCart = shoppingCartList.Select(i => i.ProductId).ToList();
            IEnumerable<Product> productList = _db.Product.Where(u => prodInCart.Contains(u.Id));

            return View(productList);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("Index")]
        public IActionResult IndexPost()
        {
            return RedirectToAction(nameof(Summary));
        }

        public IActionResult Summary()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            // var uderId = User.FindFirstValue(ClaimTypes.Name);

            List<ShoppingCart> shoppingCartList = new List<ShoppingCart>();
            if (HttpContext.Session.Get<IEnumerable<ShoppingCart>>(WC.SessionCard) != null &&
                HttpContext.Session.Get<IEnumerable<ShoppingCart>>(WC.SessionCard).Count() > 0)
            {
                // session exests
                shoppingCartList = HttpContext.Session.Get<IEnumerable<ShoppingCart>>(WC.SessionCard).ToList();
            }

            List<int> prodInCart = shoppingCartList.Select(i => i.ProductId).ToList();
            IEnumerable<Product> productList = _db.Product.Where(u => prodInCart.Contains(u.Id));

            productUserVM = new ProductUserVM()
            {
                ApplicationUser = _db.ApplicationUser.FirstOrDefault(u => u.Id == claim.Value),
                ProductList = productList.ToList(),
            };

            return View(productUserVM);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("Summary")]
        public async Task<IActionResult> SummaryPost(ProductUserVM productUserVM)
        {
            var PathToTemplate = _webHostEnvironment.WebRootPath + Path.DirectorySeparatorChar.ToString()
                + "templates" + Path.DirectorySeparatorChar.ToString() + "Inquiry.html";

            var subject = "New Inquiry";
            string HtmlBody = "";
            using (StreamReader sr = System.IO.File.OpenText(PathToTemplate))
            {
                HtmlBody = sr.ReadToEnd();
            }
            // Name: { 0}
            //Email: { 1}
            // Phone: { 2}
            //Products {3}

            StringBuilder productListSB = new StringBuilder();
            foreach (var product in productUserVM.ProductList)
            {
                productListSB.Append($" - Name: {product.Name} <span style='font-size:14px'> (ID: {product.Id})</span><br />");
            }

            string messageBody = string.Format(HtmlBody,
                productUserVM.ApplicationUser.FullName,
                productUserVM.ApplicationUser.Email,
                productUserVM.ApplicationUser.PhoneNumber,
                productListSB.ToString());

            await _emailSender.SendEmailAsync(WC.EmailAdmin, subject, messageBody);

            return RedirectToAction(nameof(InquiryConfirmation));
        }

        public IActionResult InquiryConfirmation(ProductUserVM productUserVM)
        {
            HttpContext.Session.Clear();

            return View();
        }

        public IActionResult Remove(int id)
        {
            List<ShoppingCart> shoppingCartList = new List<ShoppingCart>();
            if (HttpContext.Session.Get<IEnumerable<ShoppingCart>>(WC.SessionCard) != null &&
                HttpContext.Session.Get<IEnumerable<ShoppingCart>>(WC.SessionCard).Count() > 0)
            {
                // session exests
                shoppingCartList = HttpContext.Session.Get<IEnumerable<ShoppingCart>>(WC.SessionCard).ToList();

            }

            shoppingCartList.Remove(shoppingCartList.FirstOrDefault(u => u.ProductId == id));

            HttpContext.Session.Set(WC.SessionCard, shoppingCartList);

            return RedirectToAction(nameof(Index));
        }
    }
}

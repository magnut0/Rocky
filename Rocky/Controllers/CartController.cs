using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Rocky_DataAccess;
using Rocky_DataAccess.Repository.IRepository;
using Rocky_DataAccess.Repository;
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
        private readonly IApplicationUserRepository _userRepo;
        private readonly IProductRepository _prodRepo;
        private readonly IInquiryDetailRepository _detailRepo;
        private readonly IInquiryHeaderRepository _headerRepo;
        private readonly IOrderHeaderRepository _ordHRepo;
        private readonly IOrderDetailRepository _ordDRepo;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IEmailSender _emailSender;
        [BindProperty] // по умолчанию для POST запросов
        public ProductUserVM productUserVM { get; set; }

        public CartController(IWebHostEnvironment webHostEnvironment, IEmailSender emailSender, IProductRepository _prodRepo, IInquiryDetailRepository _detailRepo,
            IInquiryHeaderRepository _headerRepo, IApplicationUserRepository _userRepo, IOrderDetailRepository orderDetail, IOrderHeaderRepository orderHeader)
        {
            this._userRepo = _userRepo;
            this._prodRepo = _prodRepo;
            this._detailRepo = _detailRepo;
            this._headerRepo = _headerRepo;
            this._ordDRepo = orderDetail;
            this._ordHRepo = orderHeader;
            _webHostEnvironment = webHostEnvironment;
            _emailSender = emailSender;
        }

        public IActionResult Index()
        {
            // Извлекаем список товаров в корзине
            List<ShoppingCart> shoppingCartList = new List<ShoppingCart>();
            if(HttpContext.Session.Get<IEnumerable<ShoppingCart>>(WC.SessionCard) != null && 
                HttpContext.Session.Get<IEnumerable<ShoppingCart>>(WC.SessionCard).Count() > 0)
            {
                // session exests
                shoppingCartList = HttpContext.Session.Get<IEnumerable<ShoppingCart>>(WC.SessionCard).ToList();
            }

            // Select - какие столбцы выбрать
            List<int> prodInCart = shoppingCartList.Select(i => i.ProductId).ToList();
            IEnumerable<Product> productListTemp = _prodRepo.GetAll(u => prodInCart.Contains(u.Id));
            IList<Product> productList = new List<Product>();
            

            foreach (var cartObj in shoppingCartList)
            {
                Product prodTemp = productListTemp.FirstOrDefault(u => u.Id == cartObj.ProductId);
                prodTemp.TempSqft = cartObj.Sqft;
                productList.Add(prodTemp);
            }

            return View(productListTemp);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("Index")]
        public IActionResult IndexPost(IEnumerable<Product> prodList)
        {
            List<ShoppingCart> shoppingCartList = new List<ShoppingCart>();
            foreach (Product product in prodList)
            {
                shoppingCartList.Add(new ShoppingCart { ProductId = product.Id, Sqft = product.TempSqft });
            }
            HttpContext.Session.Set(WC.SessionCard, shoppingCartList);
            return RedirectToAction(nameof(Summary));
        }

        public IActionResult Summary()
        {
            ApplicationUser applicationUser;

            if(User.IsInRole(WC.AdminRole))
            {
                if(HttpContext.Session.Get<int>(WC.SessionInquiryId) != 0)
                {
                    InquiryHeader inquiryHeader = _headerRepo.FirstOrDefault(u => u.Id == HttpContext.Session.Get<int>(WC.SessionInquiryId));
                    applicationUser = new ApplicationUser()
                    {
                        Email = inquiryHeader.Email,
                        FullName = inquiryHeader.FullName,
                        PhoneNumber = inquiryHeader.PhoneNumber
                    };
                }
                else
                {
                    applicationUser = new ApplicationUser();
                }
            }
            else
            {
                var claimsIdentity = (ClaimsIdentity)User.Identity;
                var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
                // var uderId = User.FindFirstValue(ClaimTypes.Name);

                applicationUser = _userRepo.FirstOrDefault(u => u.Id == claim.Value);
            }

            List<ShoppingCart> shoppingCartList = new List<ShoppingCart>();
            if (HttpContext.Session.Get<IEnumerable<ShoppingCart>>(WC.SessionCard) != null &&
                HttpContext.Session.Get<IEnumerable<ShoppingCart>>(WC.SessionCard).Count() > 0)
            {
                // session exests
                shoppingCartList = HttpContext.Session.Get<IEnumerable<ShoppingCart>>(WC.SessionCard).ToList();
            }

            List<int> prodInCart = shoppingCartList.Select(i => i.ProductId).ToList();
            IEnumerable<Product> productList = _prodRepo.GetAll(u => prodInCart.Contains(u.Id));

            productUserVM = new ProductUserVM()
            {
                ApplicationUser = applicationUser,
            };

            foreach (var cartObj in shoppingCartList)
            {
                Product prodTemp = _prodRepo.FirstOrDefault(u => u.Id == cartObj.ProductId);
                prodTemp.TempSqft = cartObj.Sqft;
                productUserVM.ProductList.Add(prodTemp);
            }

            return View(productUserVM);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("Summary")]
        public async Task<IActionResult> SummaryPost(ProductUserVM productUserVM)
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claims = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            if(User.IsInRole(WC.AdminRole))
            {
                // create order
                //var orderTotal = 0.0;
                //foreach(Product product in productUserVM.ProductList)
                //{
                //    orderTotal += product.Price * product.TempSqft;
                //}
                OrderHeader orderHeader = new OrderHeader()
                {
                    CreatedByUserId = claims.Value,
                    // рассчёт общей стоимости нормального человека (вместо гнидышного форича)
                    FinalOrderTotal = productUserVM.ProductList.Sum(u => u.Price * u.TempSqft),
                    City = productUserVM.ApplicationUser.City,
                    StreetAddress = productUserVM.ApplicationUser.StreetAddress,
                    State = productUserVM.ApplicationUser.State,
                    PostalCode = productUserVM.ApplicationUser.PostalCode,
                    FullName = productUserVM.ApplicationUser.FullName,
                    Email = productUserVM.ApplicationUser.Email,
                    PhoneNumber = productUserVM.ApplicationUser.PhoneNumber,
                    OrderDate = DateTime.Now,
                    OrderStatus = WC.StatusPending,
                    TransactionId = "temp"
                };
                _ordHRepo.Add(orderHeader);
                _ordHRepo.Save();

                foreach (var product in productUserVM.ProductList)
                {
                    OrderDetail orderDetail = new OrderDetail()
                    {
                        OrderHeaderId = orderHeader.Id,
                        PricePerSqFt = product.Price,
                        Sqft = product.TempSqft,
                        ProductId = product.Id
                    };
                    _ordDRepo.Add(orderDetail);
                }
                _ordDRepo.Save();
                return RedirectToAction(nameof(InquiryConfirmation), new {id = orderHeader.Id});
            }
            else
            {
                // create inquiry
                var PathToTemplate = _webHostEnvironment.WebRootPath + Path.DirectorySeparatorChar.ToString()
                + "templates" + Path.DirectorySeparatorChar.ToString() + "Inquiry.html";

                var subject = "New Inquiry";
                string HtmlBody = "";
                using (StreamReader sr = System.IO.File.OpenText(PathToTemplate))
                {
                    HtmlBody = sr.ReadToEnd();
                }

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

                InquiryHeader inquiryHeader = new InquiryHeader()
                {
                    ApplicationUserId = claims.Value,
                    FullName = productUserVM.ApplicationUser.FullName,
                    Email = productUserVM.ApplicationUser.Email,
                    PhoneNumber = productUserVM.ApplicationUser.PhoneNumber,
                    InquiryDate = DateTime.Now
                };

                _headerRepo.Add(inquiryHeader);
                _headerRepo.Save();

                foreach (var product in productUserVM.ProductList)
                {
                    InquiryDetail inquiryDetail = new InquiryDetail()
                    {
                        InquiryHeaderId = inquiryHeader.Id,
                        ProductId = product.Id,
                    };
                    _detailRepo.Add(inquiryDetail);
                }
                _detailRepo.Save();
            }

            return RedirectToAction(nameof(InquiryConfirmation));
        }

        public IActionResult InquiryConfirmation(int id = 0)
        {
            OrderHeader orderHeader = _ordHRepo.FirstOrDefault(u => u.Id == id);
            HttpContext.Session.Clear();

            return View(orderHeader);
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateCart(IEnumerable<Product> prodList)
        {
            List<ShoppingCart> shoppingCartList = new List<ShoppingCart>();
            foreach(Product product in prodList)
            {
                shoppingCartList.Add(new ShoppingCart { ProductId = product.Id, Sqft = product.TempSqft });
            }
            HttpContext.Session.Set(WC.SessionCard, shoppingCartList);
            return RedirectToAction(nameof(Index));
        }
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Rocky_DataAccess;
using Rocky_Models;
using Rocky_Models.ViewModels;
using Rocky_Utility;

namespace Rocky.Controllers
{
    [Authorize(Roles = WC.AdminRole)]
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _db; // объект базы данных
        private readonly IWebHostEnvironment _webHostEnvironment; 

        public ProductController(ApplicationDbContext db, IWebHostEnvironment webHostEnvironment)
        {
            _db = db;
            _webHostEnvironment = webHostEnvironment;
        }

        public IActionResult Index()
        {
            IEnumerable<Product> objList = _db.Product.Include(prod => prod.Category).Include(prod => prod.ApplicationType);
            //foreach (var obj in objList)
            //{
            //    obj.Category = _db.Category.FirstOrDefault(u => u.Id == obj.CategoryId);
            //    obj.ApplicationType = _db.ApplicationType.FirstOrDefault(u => u.Id == obj.ApplicationTypeId);
            //};
            return View(objList);
        }
        //GET - UPSERT
        public IActionResult Create(int? id)
        {
            //IEnumerable<SelectListItem> CategoryDropDown = _db.Category.Select(i => new SelectListItem
            //{
            //    Text = i.Name,
            //    Value = i.Id.ToString()
            //});
            ////ViewBag.CategoryDropDown = CategoryDropDown;
            //ViewData["CategoryDropDown"] = CategoryDropDown;
            //Product product = new Product();
            ProductVM productVM = new ProductVM()
            {
                Product = new Product(),
                CategorySelectList = _db.Category.Select(i => new SelectListItem
                {
                    Text = i.Name,
                    Value = i.Id.ToString()
                }),
                ApplicationTypeSelectList = _db.ApplicationType.Select(i => new SelectListItem
                {
                    Text = i.Name,
                    Value = i.Id.ToString()
                })
            };
            if (id == null)
            {
                //this is for create
                return View(productVM);
            }
            else
            {
                productVM.Product = _db.Product.Find(id);
                if (productVM.Product == null)
                {
                    return NotFound();
                }
                return View(productVM);
            }
        }

        // POST - CREATE
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(ProductVM productVM)
        {
            
            var files = HttpContext.Request.Form.Files;
            string webRootPath = _webHostEnvironment.WebRootPath;
            

            //Creating
            string upload = webRootPath + WC.ImagePath;
            string fileName = Guid.NewGuid().ToString();
            string extension = Path.GetExtension(files[0].FileName);
            
            using (var fileStream = new FileStream(Path.Combine(upload, fileName + extension), FileMode.Create))
            {
                files[0].CopyTo(fileStream);
            }
            
            productVM.Product.Image = fileName + extension;
            
            _db.Product.Add(productVM.Product);

            
            _db.SaveChanges();
            return RedirectToAction("Index");
        }

        public IActionResult Edit(int id)
        {
            ProductVM productVM = new ProductVM()
            {
                Product = new Product(),
                CategorySelectList = _db.Category.Select(i => new SelectListItem
                {
                    Text = i.Name,
                    Value = i.Id.ToString()
                }),
                ApplicationTypeSelectList = _db.ApplicationType.Select(i => new SelectListItem
                {
                    Text = i.Name,
                    Value = i.Id.ToString()
                })
            };
            productVM.Product = _db.Product.Find(id);
            if (productVM.Product == null)
            {
                return NotFound();
            }
            return View(productVM);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        // POST - EDIT
        public IActionResult Edit(ProductVM productVM, int id)
        {
            var files = HttpContext.Request.Form.Files;
            string webRootPath = _webHostEnvironment.WebRootPath;

            var objFromDb = _db.Product.AsNoTracking().FirstOrDefault(u => u.Id == id);
            productVM.Product.Id = objFromDb.Id;
            productVM.Product.Image = objFromDb.Image;

            if (files.Count > 0)
            {
                string upload = webRootPath + WC.ImagePath;
                string fileName = Guid.NewGuid().ToString();
                string extension = Path.GetExtension(files[0].FileName);

                var oldFile = Path.Combine(upload, objFromDb.Image);

                if (System.IO.File.Exists(oldFile))
                {
                    System.IO.File.Delete(oldFile);
                }

                using (var fileStream = new FileStream(Path.Combine(upload, fileName + extension), FileMode.Create))
                {
                    files[0].CopyTo(fileStream);
                }

                productVM.Product.Image = fileName + extension;
            }
            else
            {
                
            }
            _db.Product.Update(productVM.Product);
            _db.SaveChanges();
            return RedirectToAction("Index");
        }

        // Get - Delete
        public IActionResult Delete(int? id)
        {
            if (id == null || id == 0) return NotFound();
            // Жадная загрузка - говорим, что также надо загрузить дополнительно из базы данных
            Product product = _db.Product.Include(prod => prod.Category).Include(prod => prod.ApplicationType).FirstOrDefault(prod => prod.Id == id);
            // product.Category = _db.Category.Find(product.CategoryId);
            if (product == null) return NotFound();

            return View(product);
        }

        // POST - Delete
        [HttpPost, ActionName("Delete")] // меняем имя пост запроса на Delete
        [ValidateAntiForgeryToken] // добавление специального токена для защиты данных
        public IActionResult DeletePost(int? id)
        {
            var product = _db.Product.Find(id);
            if (product == null) return NotFound();

            string upload = _webHostEnvironment.WebRootPath + WC.ImagePath;
            var oldFile = Path.Combine(upload, product.Image);

            if (System.IO.File.Exists(oldFile))
            {
                System.IO.File.Delete(oldFile);
            }

            _db.Product.Remove(product);
            _db.SaveChanges();
            return RedirectToAction("Index");

        }
    }
}

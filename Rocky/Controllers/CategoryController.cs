using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rocky_DataAccess;
using Rocky_Models;
using Rocky_Utility;

namespace Rocky.Controllers
{
    [Authorize(Roles = WC.AdminRole)]
    public class CategoryController : Controller
    {
        private readonly ApplicationDbContext _db; // объект базы данных

        public CategoryController(ApplicationDbContext db)
        {
            _db = db;
        }

        public IActionResult Index()
        {
            IEnumerable<Category> objList = _db.Category;
            return View(objList);
        }

        // Get - CREATE
        public IActionResult Create()
        {
            return View();
        }

        // POST - CREATE
        [HttpPost]
        [ValidateAntiForgeryToken] // добавление специального токена для защиты данных
        public IActionResult Create(Category category)
        {
            if (!ModelState.IsValid) return View(category);

            _db.Category.Add(category);
            _db.SaveChanges();
            return RedirectToAction("Index");
        }

        // Get - Edit
        public IActionResult Edit(int? id)
        {
            if(id == null || id == 0) return NotFound();
            var obj = _db.Category.Find(id);
            if (obj == null) return NotFound();

            return View(obj);
        }

        // POST - Edit
        [HttpPost]
        [ValidateAntiForgeryToken] // добавление специального токена для защиты данных
        public IActionResult Edit(Category category)
        {
            if (!ModelState.IsValid) return View(category);

            _db.Category.Update(category);
            _db.SaveChanges();
            return RedirectToAction("Index");
        }

        // Get - Delete
        public IActionResult Delete(int? id)
        {
            if (id == null || id == 0) return NotFound();
            var obj = _db.Category.Find(id);
            if (obj == null) return NotFound();

            return View(obj);
        }

        // POST - Delete
        [HttpPost]
        [ValidateAntiForgeryToken] // добавление специального токена для защиты данных
        public IActionResult DeletePost(int? id)
        {
            var obj = _db.Category.Find(id);
            if (obj == null) return NotFound(); 

            _db.Category.Remove(obj);
            _db.SaveChanges();
            return RedirectToAction("Index");

        }
    }
}

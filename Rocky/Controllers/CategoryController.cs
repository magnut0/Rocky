using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rocky_DataAccess;
using Rocky_DataAccess.Repository.IRepository;
using Rocky_Models;
using Rocky_Utility;

namespace Rocky.Controllers
{
    [Authorize(Roles = WC.AdminRole)]
    public class CategoryController : Controller
    {
        private readonly ICategoryRepository _catRepo; // объект базы данных

        public CategoryController(ICategoryRepository catRepo)
        {
            _catRepo = catRepo;
        }

        public IActionResult Index()
        {
            IEnumerable<Category> objList = _catRepo.GetAll();
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

            _catRepo.Add(category);
            _catRepo.Save();
            return RedirectToAction("Index");
        }

        // Get - Edit
        public IActionResult Edit(int? id)
        {
            if(id == null || id == 0) return NotFound();
            var obj = _catRepo.Find(id.GetValueOrDefault());
            if (obj == null) return NotFound();

            return View(obj);
        }

        // POST - Edit
        [HttpPost]
        [ValidateAntiForgeryToken] // добавление специального токена для защиты данных
        public IActionResult Edit(Category category)
        {
            if (!ModelState.IsValid) return View(category);

            _catRepo.Update(category);
            _catRepo.Save();
            return RedirectToAction("Index");
        }

        // Get - Delete
        public IActionResult Delete(int? id)
        {
            if (id == null || id == 0) return NotFound();
            var obj = _catRepo.Find(id.GetValueOrDefault());
            if (obj == null) return NotFound();

            return View(obj);
        }

        // POST - Delete
        [HttpPost]
        [ValidateAntiForgeryToken] // добавление специального токена для защиты данных
        public IActionResult DeletePost(int? id)
        {
            var obj = _catRepo.Find(id.GetValueOrDefault());
            if (obj == null) return NotFound();

            _catRepo.Remove(obj);
            _catRepo.Save();
            return RedirectToAction("Index");

        }
    }
}

﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rocky_DataAccess;
using Rocky_DataAccess.Repository;
using Rocky_DataAccess.Repository.IRepository;
using Rocky_Models;
using Rocky_Utility;

namespace Rocky.Controllers
{
    [Authorize(Roles = WC.AdminRole)]
    public class ApplicationTypeController : Controller
    {
        private readonly IApplicationTypeRepository _appTypeRepo;
        public ApplicationTypeController(IApplicationTypeRepository appTypeRepo)
        {
            _appTypeRepo = appTypeRepo;
        }
        public IActionResult Index()
        {
            IEnumerable<ApplicationType> objList = _appTypeRepo.GetAll();
            return View(objList);
        }

        public IActionResult Create()
        {
            return View();
        }

        // POST - CREATE
        [HttpPost]
        [ValidateAntiForgeryToken] // добавление специального токена для защиты данных
        public IActionResult Create(ApplicationType test)
        {
            if (!ModelState.IsValid) return View(test);

            _appTypeRepo.Add(test);
            _appTypeRepo.Save();
            return RedirectToAction("Index");
        }

        // Get - Edit
        public IActionResult Edit(int? id)
        {
            if (id == null || id == 0) return NotFound();
            var obj = _appTypeRepo.Find(id.GetValueOrDefault());
            if (obj == null) return NotFound();

            return View(obj);
        }

        // POST - Edit
        [HttpPost]
        [ValidateAntiForgeryToken] // добавление специального токена для защиты данных
        public IActionResult Edit(ApplicationType test)
        {
            if (!ModelState.IsValid) return View(test);

            _appTypeRepo.Update(test);
            _appTypeRepo.Save();
            return RedirectToAction("Index");
        }

        // Get - Delete
        public IActionResult Delete(int? id)
        {
            if (id == null || id == 0) return NotFound();
            var obj = _appTypeRepo.Find(id.GetValueOrDefault());
            if (obj == null) return NotFound();

            return View(obj);
        }

        // POST - Delete
        [HttpPost]
        [ValidateAntiForgeryToken] // добавление специального токена для защиты данных
        public IActionResult DeletePost(int? id)
        {
            var obj = _appTypeRepo.Find(id.GetValueOrDefault());
            if (obj == null) return NotFound();

            _appTypeRepo.Remove(obj);
            _appTypeRepo.Save();
            return RedirectToAction("Index");

        }
    }
}

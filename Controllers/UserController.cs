using LordMarket.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace LordMarket.Controllers
{
    public class UserController : Controller
    {
        LordMarketDBEntities db = new LordMarketDBEntities();

        [Authorize]
        public ActionResult Kullanici()
        {
            var Kullanici = db.Kullanicilar.ToList();
            return View(Kullanici);
        }

        [Authorize]
        [HttpGet]
        public ActionResult YeniKullanici()
        {
            return View();
        }

        [Authorize]
        [HttpPost]
        public ActionResult YeniKullanici(Kullanicilar Kullanici)
        {
            if (ModelState.IsValid)
            {
                Kullanici.Status = true;
                Kullanici.SonGuncellenmeTarihi = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
                db.Kullanicilar.Add(Kullanici);
                db.SaveChanges();
                return RedirectToAction("Kullanici");
            }

            return View(Kullanici);
        }

        [Authorize]
        public ActionResult KullaniciSil(int id)
        {
            var Kullanici = db.Kullanicilar.Find(id);
            if (Kullanici != null)
            {
                Kullanici.Status = false;
                db.SaveChanges();
            }
            return RedirectToAction("Kullanici");
        }

        [Authorize]
        public ActionResult KullaniciGetir(int id)
        {
            var Kullanici = db.Kullanicilar.Find(id);
            if (Kullanici == null) return HttpNotFound();

            return View(Kullanici);
        }

        [Authorize]
        [HttpPost]
        public ActionResult KullaniciGuncelle(Kullanicilar y)
        {
            if (ModelState.IsValid)
            {
                var Kullanici = db.Kullanicilar.Find(y.ID);
                if (Kullanici == null) return HttpNotFound();

                Kullanici.Username = y.Username;
                Kullanici.Password = y.Password;
                Kullanici.Image = y.Image;
                Kullanici.Role = y.Role;
                Kullanici.SonGuncellenmeTarihi = DateTime.Now.ToString("dd/MM/yyyy HH:mm");



                Kullanici.Status = y.Status;

                db.SaveChanges();
                return RedirectToAction("Kullanici");
            }

            return View("KullaniciGetir", y);
        }
    }
}
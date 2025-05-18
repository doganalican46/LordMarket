using LordMarket.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace LordMarket.Controllers
{
    public class KullaniciController : Controller
    {
        LordMarketDBEntities db = new LordMarketDBEntities();

        // Listeleme
        public ActionResult Kullanicilar()
        {
            var Kullanicilar = db.Kullanicilar.ToList();
            return View(Kullanicilar);
        }

        // Yeni Kullanici formu
        [HttpGet]
        public ActionResult YeniKullanici()
        {
            return View();
        }

        // Yeni Kullanici kaydetme
        [HttpPost]
        public ActionResult YeniKullanici(Kullanicilar Kullanici)
        {
            if (ModelState.IsValid)
            {
                Kullanici.Status = true;
                Kullanici.SonGuncellenmeTarihi = DateTime.Now.ToString();
                db.Kullanicilar.Add(Kullanici);
                db.SaveChanges();
                return RedirectToAction("Kullanicilar");
            }

            return View(Kullanici);
        }

        // Silme (Pasif yapma)
        public ActionResult KullaniciSil(int id)
        {
            var Kullanici = db.Kullanicilar.Find(id);
            if (Kullanici != null)
            {
                Kullanici.Status = false;
                db.SaveChanges();
            }
            return RedirectToAction("Kullanicilar");
        }

        // Güncelleme formu
        public ActionResult KullaniciGetir(int id)
        {
            var Kullanici = db.Kullanicilar.Find(id);
            if (Kullanici == null) return HttpNotFound();

            return View(Kullanici);
        }

        // Güncelleme işlemi
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
                Kullanici.SonGuncellenmeTarihi = DateTime.Now.ToString();
                Kullanici.Status = y.Status;

                db.SaveChanges();
                return RedirectToAction("Kullanicilar");
            }

            return View("KullaniciGetir", y);
        }



        
    }
}
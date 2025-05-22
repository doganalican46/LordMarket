using LordMarket.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Net;
using System.Web;
using System.Web.Mvc;

namespace LordMarket.Controllers
{
    public class KullaniciController : Controller
    {
        LordMarketDBEntities db = new LordMarketDBEntities();

        [Authorize]
        public ActionResult Kullanicilar()
        {
            var Kullanicilar = db.Kullanicilar.ToList();
            return View(Kullanicilar);
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
                Kullanici.SonGuncellenmeTarihi = DateTime.Now.ToString();
                db.Kullanicilar.Add(Kullanici);
                db.SaveChanges();
                return RedirectToAction("Kullanicilar");
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
            return RedirectToAction("Kullanicilar");
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
                Kullanici.SonGuncellenmeTarihi = DateTime.Now.ToString();
                Kullanici.Status = y.Status;

                db.SaveChanges();
                return RedirectToAction("Kullanicilar");
            }

            return View("KullaniciGetir", y);
        }


        [Authorize]
        public ActionResult Toptancilar()
        {
            var Toptancilar = db.Kullanicilar.Where(x => x.Role == "toptanci").ToList();
            return View(Toptancilar);
        }

        [Authorize]
        [HttpGet]
        public ActionResult YeniToptanci()
        {
            return View();
        }

        [Authorize]
        [HttpPost]
        public ActionResult YeniToptanci(Kullanicilar Kullanici)
        {
            if (ModelState.IsValid)
            {
                Kullanici.Status = true;
                Kullanici.Role = "toptanci";
                
                Kullanici.SonGuncellenmeTarihi = DateTime.Now.ToString();
                db.Kullanicilar.Add(Kullanici);
                db.SaveChanges();
                return RedirectToAction("Toptancilar");
            }

            return View(Kullanici);
        }

        [Authorize]
        public ActionResult ToptanciSil(int id)
        {
            var Kullanici = db.Kullanicilar.Find(id);
            if (Kullanici != null)
            {
                Kullanici.Status = false;
                db.SaveChanges();
            }
            return RedirectToAction("Toptancilar");
        }

        [Authorize]
        public ActionResult ToptanciKaldir(int id)
        {
            var toptanci = db.Kullanicilar.Find(id);
            if (toptanci != null)
            {
                db.Kullanicilar.Remove(toptanci);
                db.SaveChanges();
            }
            return RedirectToAction("Toptancilar");
        }

        [Authorize]
        public ActionResult ToptanciGetir(int id)
        {
            var Kullanici = db.Kullanicilar.Find(id);
            if (Kullanici == null) return HttpNotFound();

            return View(Kullanici);
        }

        [Authorize]
        [HttpPost]
        public ActionResult ToptanciGuncelle(Kullanicilar y)
        {
            if (ModelState.IsValid)
            {
                var Kullanici = db.Kullanicilar.Find(y.ID);
                if (Kullanici == null) return HttpNotFound();

                Kullanici.Username = y.Username;
                Kullanici.Password = y.Password;
                Kullanici.Image = y.Image;
                Kullanici.SonGuncellenmeTarihi = DateTime.Now.ToString();
                Kullanici.Status = y.Status;

                db.SaveChanges();
                return RedirectToAction("Toptancilar");
            }

            return View("ToptanciGetir", y);
        }



        //pofz rmri bmnl odgb

        









    }
}
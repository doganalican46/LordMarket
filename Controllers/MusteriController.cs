using LordMarket.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace LordMarket.Controllers
{
    public class MusteriController : Controller
    {
        LordMarketDBEntities db = new LordMarketDBEntities();

        // Listeleme
        public ActionResult Musteriler()
        {
            var Musteriler = db.Musteriler.ToList();
            return View(Musteriler);
        }

        // Yeni ürün formu
        [HttpGet]
        public ActionResult YeniMusteri()
        {
            return View();
        }

        // Yeni ürün kaydetme
        [HttpPost]
        public ActionResult YeniMusteri(Musteriler Musteri)
        {
            if (ModelState.IsValid)
            {
                Musteri.Status = true;
                Musteri.SonGuncellenmeTarihi = DateTime.Now.ToString("dd/MM/yyyy");
                db.Musteriler.Add(Musteri);
                db.SaveChanges();
                return RedirectToAction("Musteriler");
            }

            return View(Musteri);
        }

        // Silme (Pasif yapma)
        public ActionResult MusteriSil(int id)
        {
            var Musteri = db.Musteriler.Find(id);
            if (Musteri != null)
            {
                Musteri.Status = false;
                db.SaveChanges();
            }
            return RedirectToAction("Musteriler");
        }

        // Güncelleme formu
        public ActionResult MusteriGetir(int id)
        {
            var Musteri = db.Musteriler.Find(id);
            if (Musteri == null) return HttpNotFound();

            return View(Musteri);
        }

        // Güncelleme işlemi
        [HttpPost]
        public ActionResult MusteriGuncelle(Musteriler y)
        {
            if (ModelState.IsValid)
            {
                var Musteri = db.Musteriler.Find(y.ID);
                if (Musteri == null) return HttpNotFound();

                Musteri.MusteriAdSoyad = y.MusteriAdSoyad;
                Musteri.SatisIslemID = y.SatisIslemID;
                Musteri.Notlar = y.Notlar;
                Musteri.ToplamBorc = y.ToplamBorc;
                Musteri.SonGuncellenmeTarihi = DateTime.Now.ToString("dd/MM/yyyy");
                Musteri.Status = y.Status;

                db.SaveChanges();
                return RedirectToAction("Musteriler");
            }

            return View("MusteriGetir", y);
        }

        
    }
}
using LordMarket.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace LordMarket.Controllers
{
    public class KategoriController : Controller
    {
        LordMarketDBEntities db = new LordMarketDBEntities();

        // Listeleme
        public ActionResult Kategoriler()
        {
            var kategoriler = db.Kategoriler.Where(u => u.Status == true).ToList();
            return View(kategoriler);
        }

        // Yeni ürün formu
        [HttpGet]
        public ActionResult YeniKategori()
        {
            return View();
        }

        // Yeni ürün kaydetme
        [HttpPost]
        public ActionResult YeniKategori(Kategoriler Kategori)
        {
            if (ModelState.IsValid)
            {
                Kategori.Status = true;
                Kategori.SonGuncellemeTarihi =DateTime.Now.ToString("dd/MM/yyyy HH:mm");
                db.Kategoriler.Add(Kategori);
                db.SaveChanges();
                return RedirectToAction("Kategoriler");
            }

            return View(Kategori);
        }

        // Silme (Pasif yapma)
        public ActionResult KategoriSil(int id)
        {
            var Kategori = db.Kategoriler.Find(id);
            if (Kategori != null)
            {
                Kategori.Status = false;
                db.SaveChanges();
            }
            return RedirectToAction("Kategoriler");
        }

        // Güncelleme formu
        public ActionResult KategoriGetir(int id)
        {
            var Kategori = db.Kategoriler.Find(id);
            if (Kategori == null) return HttpNotFound();

            return View(Kategori);
        }

        // Güncelleme işlemi
        [HttpPost]
        public ActionResult KategoriGuncelle(Kategoriler y)
        {
            if (ModelState.IsValid)
            {
                var Kategori = db.Kategoriler.Find(y.ID);
                if (Kategori == null) return HttpNotFound();

                Kategori.KategoriAd = y.KategoriAd;
              
                Kategori.SonGuncellemeTarihi =DateTime.Now.ToString("dd/MM/yyyy HH:mm");
                Kategori.Status = y.Status;

                db.SaveChanges();
                return RedirectToAction("Kategoriler");
            }

            return View("KategoriGetir", y);
        }

       
    }
}
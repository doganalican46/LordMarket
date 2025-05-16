using LordMarket.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace LordMarket.Controllers
{
    public class UrunController : Controller
    {
        LordMarketDBEntities db = new LordMarketDBEntities();

        // Listeleme
        public ActionResult Urunler()
        {
            //ViewBag.Kategoriler = GetKategoriSelectList();
            var urunler = db.Urunler.ToList();
            return View(urunler);
        }

        public ActionResult HizliUrunler()
        {
            var urunler = db.Urunler.Where(m=>m.HizliUrunMu==true).ToList();
            return View(urunler);
        }

        // Yeni ürün formu
        [HttpGet]
        public ActionResult YeniUrun()
        {
            return View();
        }

        // Yeni ürün kaydetme
        [HttpPost]
        public ActionResult YeniUrun(Urunler urun)
        {
            if (ModelState.IsValid)
            {
                urun.Status = true;
                urun.EklenmeTarihi = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
                db.Urunler.Add(urun);
                db.SaveChanges();
                return RedirectToAction("Urunler");
            }

            return View(urun);
        }

        // Silme (Pasif yapma)
        public ActionResult UrunSil(int id)
        {
            var urun = db.Urunler.Find(id);
            if (urun != null)
            {
                urun.Status = false;
                db.SaveChanges();
            }
            return RedirectToAction("Urunler");
        }

        // Güncelleme formu
        public ActionResult UrunGetir(int id)
        {
            var urun = db.Urunler.Find(id);
            if (urun == null) return HttpNotFound();

            return View(urun);
        }

        // Güncelleme işlemi
        [HttpPost]
        public ActionResult UrunGuncelle(Urunler y)
        {
            if (ModelState.IsValid)
            {
                var urun = db.Urunler.Find(y.ID);
                if (urun == null) return HttpNotFound();

                urun.Barkod = y.Barkod;
                urun.UrunAd = y.UrunAd;
                urun.UrunFiyat = y.UrunFiyat;
                urun.UrunResmi = y.UrunResmi;
                urun.KDVOran = y.KDVOran;
                urun.Stok = y.Stok;
                urun.GuncellenmeTarihi = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
                urun.Status = y.Status;

                db.SaveChanges();

                TempData["GuncellemeBasarili"] = "Ürün başarıyla güncellendi!";
                return RedirectToAction("Urunler");
            }

            return View("UrunGetir", y);
        }


        [HttpGet]
        public ActionResult HizliUrunYap(int id)
        {
            var urun = db.Urunler.Find(id);
            if (urun == null)
                return HttpNotFound();

            // Toggle işlemi
            urun.HizliUrunMu = !urun.HizliUrunMu;
            urun.GuncellenmeTarihi = DateTime.Now.ToString("dd/MM/yyyy HH:mm");

            db.SaveChanges();
            return RedirectToAction("Urunler");
        }



        public ActionResult EtiketOlustur()
        {
            var urunler = db.Urunler.ToList();
            return View(urunler);
        }



        // Kategorileri dropdown için getirir
        //private List<SelectListItem> GetKategoriSelectList()
        //{
        //    return db.Kategoriler
        //        .Where(k => k.Status == true)
        //        .Select(k => new SelectListItem
        //        {
        //            Text = k.KategoriAd,
        //            Value = k.ID.ToString()
        //        }).ToList();
        //}

    }
}
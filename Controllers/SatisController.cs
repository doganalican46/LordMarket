using LordMarket.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace LordMarket.Controllers
{
    public class SatisController : Controller
    {
        LordMarketDBEntities db = new LordMarketDBEntities();

        // Listeleme
        public ActionResult Satis()
        {
            var Satis = db.Satislar.ToList();
            return View(Satis);
        }

        // Yeni ürün formu
        [HttpGet]
        public ActionResult YeniSatis()
        {
            return View();
        }

        // Yeni ürün kaydetme
        [HttpPost]
        public ActionResult YeniSatis(Satislar Satis)
        {
            if (ModelState.IsValid)
            {
                Satis.Status = true;
                Satis.SatisTarihi = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
                db.Satislar.Add(Satis);
                db.SaveChanges();
                return RedirectToAction("Satis");
            }

            return View(Satis);
        }

        // Silme (Pasif yapma)
        public ActionResult SatisSil(int id)
        {
            var Satis = db.Satislar.Find(id);
            if (Satis != null)
            {
                Satis.Status = false;
                db.SaveChanges();
            }
            return RedirectToAction("Satis");
        }

        // Güncelleme formu
        public ActionResult SatisGetir(int id)
        {
            var Satis = db.Satislar.Find(id);
            if (Satis == null) return HttpNotFound();

            return View(Satis);
        }

        // Güncelleme işlemi
        [HttpPost]
        public ActionResult SatisGuncelle(Satislar y)
        {
            if (ModelState.IsValid)
            {
                var Satis = db.Satislar.Find(y.ID);
                if (Satis == null) return HttpNotFound();

                Satis.UrunID = y.UrunID;
                Satis.SatisIslemID = y.SatisIslemID;
                Satis.Tutar = y.Tutar;
                Satis.SatisTarihi = DateTime.Now.ToString("dd/MM/yyyy HH:mm");

                Satis.Status = y.Status;

                db.SaveChanges();
                return RedirectToAction("Satis");
            }

            return View("SatisGetir", y);
        }
    }
}
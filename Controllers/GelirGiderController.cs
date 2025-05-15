using LordMarket.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace LordMarket.Controllers
{
    public class GelirGiderController : Controller
    {
        LordMarketDBEntities db = new LordMarketDBEntities();

        // Listeleme
        public ActionResult GelirGider()
        {
            var GelirGider = db.GelirGider.ToList();
            return View(GelirGider);
        }

        // Yeni ürün formu
        [HttpGet]
        public ActionResult YeniGelirGider()
        {
            return View();
        }

        // Yeni ürün kaydetme
        [HttpPost]
        public ActionResult YeniGelirGider(GelirGider GelirGider)
        {
            if (ModelState.IsValid)
            {
                GelirGider.Status = true;
                GelirGider.Tarih =DateTime.Now;
                db.GelirGider.Add(GelirGider);
                db.SaveChanges();
                return RedirectToAction("GelirGider");
            }

            return View(GelirGider);
        }

        // Silme (Pasif yapma)
        public ActionResult GelirGiderSil(int id)
        {
            var GelirGider = db.GelirGider.Find(id);
            if (GelirGider != null)
            {
                GelirGider.Status = false;
                db.SaveChanges();
            }
            return RedirectToAction("GelirGider");
        }

        // Güncelleme formu
        public ActionResult GelirGiderGetir(int id)
        {
            var GelirGider = db.GelirGider.Find(id);
            if (GelirGider == null) return HttpNotFound();

            return View(GelirGider);
        }

        // Güncelleme işlemi
        [HttpPost]
        public ActionResult GelirGiderGuncelle(GelirGider y)
        {
            if (ModelState.IsValid)
            {
                var GelirGider = db.GelirGider.Find(y.ID);
                if (GelirGider == null) return HttpNotFound();

                GelirGider.Tur = y.Tur;
                GelirGider.Tutar = y.Tutar;
                GelirGider.Notlar = y.Notlar;
               
                GelirGider.Status = y.Status;

                db.SaveChanges();
                return RedirectToAction("GelirGider");
            }

            return View("GelirGiderGetir", y);
        }
    }
}
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

        [Authorize]
        public ActionResult Musteriler()
        {
            var Musteriler = db.Musteriler.Where(x => x.Status == true).ToList();
            return View(Musteriler);
        }

        [Authorize]
        [HttpGet]
        public ActionResult YeniMusteri()
        {
            return View();
        }

        [Authorize]
        [HttpPost]
        public ActionResult YeniMusteri(Musteriler Musteri)
        {
            if (ModelState.IsValid)
            {
                Musteri.Status = true;
                Musteri.SonGuncellenmeTarihi =DateTime.Now;
                db.Musteriler.Add(Musteri);
                db.SaveChanges();
                return RedirectToAction("Musteriler");
            }

            return View(Musteri);
        }

        [Authorize]
        public ActionResult MusteriSil(int id)
        {
            var Musteri = db.Musteriler.Find(id);
            if (Musteri != null)
            {
                Musteri.ToplamBorc = 0;
                Musteri.Status = false;
                db.SaveChanges();
            }
            return RedirectToAction("Musteriler");
        }

       

        [Authorize]
        public ActionResult MusteriGetir(int id)
        {
            var Musteri = db.Musteriler.Find(id);
            if (Musteri == null) return HttpNotFound();

            return View(Musteri);
        }

        [Authorize]
        [HttpPost]
        public ActionResult MusteriGuncelle(Musteriler y)
        {
            if (ModelState.IsValid)
            {
                var Musteri = db.Musteriler.Find(y.ID);
                if (Musteri == null) return HttpNotFound();

                Musteri.MusteriAdSoyad = y.MusteriAdSoyad;
                Musteri.Notlar = y.Notlar;
                Musteri.ToplamBorc = y.ToplamBorc;
                Musteri.SonGuncellenmeTarihi = DateTime.Now;
                Musteri.Status = y.Status;

                db.SaveChanges();
                return RedirectToAction("Musteriler");
            }

            return View("MusteriGetir", y);
        }


        [Authorize]
        [HttpPost]
        public ActionResult BorcOde(int id, decimal OdenenBorcTutar, string Not)
        {
            var musteri = db.Musteriler.Find(id);
            if (musteri == null) return HttpNotFound();

            musteri.ToplamBorc -= OdenenBorcTutar;

            string bilgi = $"Borç Ödemesi: {DateTime.Now:yyyy-MM-dd HH:mm} - {OdenenBorcTutar} ₺ Not: {Not} ||";
            musteri.BosAlan += bilgi;

            db.SaveChanges();
            return RedirectToAction("Musteriler");
        }

        [Authorize]
        [HttpPost]
        public ActionResult VeresiyeEkle(int id, decimal VeresiyeTutar, string Not)
        {
            var musteri = db.Musteriler.Find(id);
            if (musteri == null) return HttpNotFound();

            musteri.ToplamBorc += VeresiyeTutar;

            string bilgi = $"Veresiye - {DateTime.Now:yyyy-MM-dd HH:mm} - {VeresiyeTutar} ₺ Not: {Not} ||";
            musteri.BosAlan += bilgi;

            db.SaveChanges();
            return RedirectToAction("Musteriler");
        }


        [Authorize]
        [HttpPost]
        public ActionResult MusteriTemizle(int id)
        {
            var urun = db.Musteriler.Find(id);
            if (urun == null) return HttpNotFound();


            urun.BosAlan = "";
            urun.Notlar = "";
            urun.ToplamBorc = 0;


            db.SaveChanges();

            return RedirectToAction("MusteriGetir", new { id = id });
        }




    }
}
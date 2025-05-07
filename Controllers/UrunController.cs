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



        
        public ActionResult Urunler()
        {
            var urun = db.Urunler.ToList();
            return View(urun);
        }


        
        [HttpGet]
        public ActionResult YeniUrun()
        {
            List<SelectListItem> Kategoriler = (from i in db.Kategoriler.Where(k => k.Status == true).ToList()
                                             select new SelectListItem
                                             {
                                                 Text = i.KategoriAd,
                                                 Value = i.ID.ToString()
                                             }).ToList();
            ViewBag.Kategoriler = Kategoriler;
            return View();
        }
        
        [HttpPost]
        public ActionResult YeniUrun(Urunler y, int selectedKategoriID)
        {
            List<SelectListItem> Kategoriler = (from i in db.Kategoriler.Where(k => k.Status == true).ToList()
                                             select new SelectListItem
                                             {
                                                 Text = i.KategoriAd,
                                                 Value = i.ID.ToString()
                                             }).ToList();
            ViewBag.Kategoriler = Kategoriler;

            y.KategoriID = selectedKategoriID;

            db.Urunler.Add(y);
            db.SaveChanges();
            return RedirectToAction("Urunler");
        }

        
        public ActionResult UrunSil(int id)
        {
            var urun = db.Urunler.Find(id);
            urun.Status = false;
            db.SaveChanges();
            return RedirectToAction("Urunler");
        }
        
        public ActionResult UrunGetir(int id)
        {
            var urun = db.Urunler.Find(id);
            if (urun == null)
            {
                return HttpNotFound();
            }

            List<SelectListItem> Kategoriler = (from i in db.Kategoriler.Where(k => k.Status == true).ToList()
                                             select new SelectListItem
                                             {
                                                 Text = i.KategoriAd,
                                                 Value = i.ID.ToString()
                                             }).ToList();
            ViewBag.Kategoriler = Kategoriler;

            return View("UrunGetir", urun);
        }

        
        [HttpPost]
        public ActionResult UrunGuncelle(Urunler y)
        {
            if (ModelState.IsValid)
            {
                var urun = db.Urunler.Find(y.ID);
                if (urun != null)
                {
                    urun.UrunAd = y.UrunAd;
                    urun.UrunFiyat = y.UrunFiyat;
                    urun.Stok = y.Stok;
                    urun.Barkod = y.Barkod;
                    urun.GuncellenmeTarihi = y.GuncellenmeTarihi;
                    urun.KategoriID = y.KategoriID;
                    urun.KDVOran = y.KDVOran;

                    db.SaveChanges();
                    return RedirectToAction("Urunler");
                }
                return HttpNotFound();
            }

            List<SelectListItem> Kategoriler = (from i in db.Kategoriler.Where(k => k.Status == true).ToList()
                                             select new SelectListItem
                                             {
                                                 Text = i.KategoriAd,
                                                 Value = i.ID.ToString()
                                             }).ToList();
            ViewBag.Kategoriler = Kategoriler;

            return View("UrunGetir", y);
        }




    }
}
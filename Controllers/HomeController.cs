using LordMarket.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace LordMarket.Controllers
{
    public class HomeController : Controller
    {
        LordMarketDBEntities db = new LordMarketDBEntities();

        public class SatisIslemViewModel1
        {
            public List<GelirGider> GelirGider { get; set; }
            public List<Kategoriler> Kategoriler { get; set; }
            public List<Musteriler> Musteriler { get; set; }
            public List<Satislar> Satislar { get; set; }
            public List<SatisIslem> SatisIslem { get; set; }
            public List<Urunler> Urunler { get; set; }

        }


        // GET: SatisYap
        public ActionResult Index(string barkod = null)
        {
            var viewModel = new SatisIslemViewModel1
            {
                GelirGider = db.GelirGider.Where(h => h.Status == true).ToList(),
                Kategoriler = db.Kategoriler.Where(h => h.Status == true).ToList(),
                Musteriler = db.Musteriler.Where(h => h.Status == true).ToList(),
                Satislar = db.Satislar.Where(h => h.Status == true).ToList(),
                SatisIslem = db.SatisIslem.Where(h => h.Status == true).ToList(),
                Urunler = string.IsNullOrEmpty(barkod)
                    ? new List<Urunler>()
                    : db.Urunler.Where(u => u.Barkod == barkod && u.Status == true).ToList()
            };

            ViewBag.Kategoriler = GetKategoriSelectList();
            return View(viewModel);
        }


        private List<SelectListItem> GetKategoriSelectList()
        {
            return db.Kategoriler
                .Where(k => k.Status == true)
                .Select(k => new SelectListItem
                {
                    Text = k.KategoriAd,
                    Value = k.ID.ToString()
                }).ToList();
        }










        [HttpGet]
        public ActionResult YeniUrun()
        {
            ViewBag.Kategoriler = GetKategoriSelectList();
            return View();
        }

        [HttpPost]
        public ActionResult YeniUrun(Urunler urun)
        {
            if (ModelState.IsValid)
            {
                urun.Status = true;
                urun.EklenmeTarihi = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
                db.Urunler.Add(urun);
                db.SaveChanges();

                // Set success message in TempData
                TempData["SuccessMessage"] = "Ürün başarıyla eklendi!";

                return RedirectToAction("Index");
            }

            ViewBag.Kategoriler = GetKategoriSelectList();
            return View(urun);
        }



        [HttpPost]
        public ActionResult BarkodAra(string barkod)
        {
            var urun = db.Urunler.FirstOrDefault(u => u.Barkod == barkod);

            var model = new SatisIslemViewModel1
            {
                Urunler = urun != null ? new List<Urunler> { urun } : new List<Urunler>()
            };

            return View("Index", model); // Aynı sayfaya dönüyoruz
        }

        [HttpGet]
        public ActionResult BarkodAra()
        {
            return View(new List<Urunler>());
        }

    }
}
using LordMarket.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace LordMarket.Controllers
{
    public class SatisYapController : Controller
    {
        LordMarketDBEntities db = new LordMarketDBEntities();

        public class SatisIslemViewModel
        {
            public List<GelirGider> GelirGider { get; set; }
            public List<Musteriler> Musteriler { get; set; }
            public List<SatisIslem> SatisIslem { get; set; }
            public List<Urunler> Urunler { get; set; }

        }


        // GET: SatisYap
        public ActionResult Index()
        {
            var viewModel = new SatisIslemViewModel
            {
                GelirGider = db.GelirGider.Where(h => h.Status == true).ToList(),
                Musteriler = db.Musteriler.Where(h => h.Status == true).ToList(),
                SatisIslem = db.SatisIslem.Where(h => h.Status == true).ToList(),
                Urunler = db.Urunler.Where(h => h.Status == true).ToList()
            };



            return View(viewModel); // Eksik olan kısım burasıydı
        }












        [HttpGet]
        public ActionResult YeniUrun()
        {
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

            return View(urun);
        }



    }
}
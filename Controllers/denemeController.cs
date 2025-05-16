using LordMarket.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace LordMarket.Controllers
{
    public class denemeController : Controller
    {
        LordMarketDBEntities db = new LordMarketDBEntities();

        public class SatisIslemViewModel1
        {
            public List<GelirGider> GelirGider { get; set; }
            public List<Musteriler> Musteriler { get; set; }
            public List<SatisIslem> SatisIslem { get; set; }
            public List<Urunler> Urunler { get; set; }
            public List<Urunler> HizliUrunler { get; set; }


        }



        public ActionResult Index()
        {
            var viewModel = new SatisIslemViewModel1
            {
                GelirGider = db.GelirGider.Where(h => h.Status == true).ToList(),
                Musteriler = db.Musteriler.Where(h => h.Status == true).ToList(),
                SatisIslem = db.SatisIslem.Where(h => h.Status == true).ToList(),
                Urunler = db.Urunler.Where(h => h.Status == true).ToList(),
                HizliUrunler = db.Urunler.Where(h => h.Status == true && h.HizliUrunMu == true).ToList(),

            };

            ViewBag.MusteriListesi = db.Musteriler
            .Select(m => new
            {
                ID = m.ID,
                MusteriAdSoyad = m.MusteriAdSoyad
            })
            .ToList();

            return View(viewModel);
        }


        [HttpPost]
        public JsonResult BarkodAra(string barkod)
        {
            var urun = db.Urunler.FirstOrDefault(x => x.Barkod == barkod && x.Status == true);

            if (urun != null)
            {
                return Json(new
                {
                    success = true,
                    urunAd = urun.UrunAd,
                    fiyat = urun.UrunFiyat
                });
            }

            return Json(new { success = false });
        }


        [HttpPost]
        public JsonResult SatisYap(string OdemeTipi, decimal ToplamTutar, string UrunListesi, int? MusteriID)
        {
            ViewBag.MusteriListesi = db.Musteriler
            .Select(m => new
            {
                ID = m.ID,
                MusteriAdSoyad = m.MusteriAdSoyad
            })
            .ToList();
            try
            {
                SatisIslem yeniSatis = new SatisIslem
                {
                    ToplamTutar = ToplamTutar,
                    OdemeTipi = OdemeTipi,
                    UrunListesi = UrunListesi,
                    Tarih = DateTime.Now,
                    Status = true,
                    MusteriID = MusteriID
                };

                db.SatisIslem.Add(yeniSatis);

                // Veresiye işlemi ise müşteriye borç yaz ve notları güncelle
                if (OdemeTipi == "Veresiye" && MusteriID.HasValue)
                {
                    var musteri = db.Musteriler.FirstOrDefault(m => m.ID == MusteriID.Value && m.Status == true);
                    if (musteri != null)
                    {
                        musteri.ToplamBorc += ToplamTutar;
                        musteri.Notlar += "\n" + UrunListesi;
                        musteri.SonGuncellenmeTarihi = DateTime.Now;
                        db.Entry(musteri).State = System.Data.Entity.EntityState.Modified;
                    }
                }

                db.SaveChanges();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
















    }
}
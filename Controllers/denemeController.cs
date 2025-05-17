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
            UpdateSatisToplamTutar();

            var viewModel = new SatisIslemViewModel1
            {
                GelirGider = db.GelirGider.Where(h => h.Status == true).ToList(),
                Musteriler = db.Musteriler.Where(h => h.Status == true).ToList(),
                SatisIslem = db.SatisIslem.Where(h => h.Status == true).ToList(),
                Urunler = db.Urunler.Where(h => h.Status == true).ToList(),
                HizliUrunler = db.Urunler.Where(h => h.Status == true && h.HizliUrunMu == true).ToList(),

            };

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
        public JsonResult SatisYap(string OdemeTipi, decimal? ToplamTutar, string UrunListesi, int? MusteriID)
        {
            try
            {
                // Veresiye seçildiğinde müşteri ID kontrolü zorunlu
                if (OdemeTipi == "Veresiye" && (MusteriID == null || MusteriID == 0))
                {
                    return Json(new { success = false, message = "Veresiye satış için müşteri seçimi zorunludur." });
                }

                // Ürün listesi boş olamaz
                if (string.IsNullOrWhiteSpace(UrunListesi))
                {
                    return Json(new { success = false, message = "Ürün listesi boş olamaz." });
                }

                SatisIslem yeniSatis = new SatisIslem
                {
                    ToplamTutar = ToplamTutar,
                    OdemeTipi = OdemeTipi,
                    UrunListesi = UrunListesi,
                    Tarih = DateTime.Now,
                    Status = true,
                    MusteriID = (OdemeTipi == "Veresiye") ? MusteriID : null
                };

                db.SatisIslem.Add(yeniSatis);
                db.SaveChanges();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Hata oluştu: " + ex.Message });
            }
        }

        private void UpdateSatisToplamTutar()
        {
            var satislar = db.SatisIslem
                            .Where(s => s.Status == true && (!s.ToplamTutar.HasValue || s.ToplamTutar == 0))
                            .ToList();

            foreach (var satis in satislar)
            {
                if (string.IsNullOrWhiteSpace(satis.UrunListesi))
                    continue;

                decimal toplam = 0;

                // Ürün listesini parçala
                // Örnek: "Kristal Bardak - 1 Adet - 1.5₺ Maraş Otu - 1 Adet - 7₺ Benimo - 1 Adet - 28₺"
                // Ürünler arası boşluklarla ayrılmış ama ürünler kendi içinde " - " ile ayrılmış.
                // En sağdaki fiyat kısmını alacağız.

                var urunler = satis.UrunListesi.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);

                // Daha sağlam çözüm için " - " ile ayırıp fiyatı almak gerek.
                // Bu nedenle her ürünün "İsim - adet - fiyat₺" şeklinde ayrılması gerekiyor.
                // Bunu ayırmak için, UrunListesi stringini "₺" işaretine göre split edip her ürün fiyatını alabiliriz.

                // Alternatif olarak ürünleri '₺' işaretinden bölüp işlem yapalım:

                var urunParcalari = satis.UrunListesi.Split(new char[] { '₺' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var parca in urunParcalari)
                {
                    // Her parça örn: "Kristal Bardak - 1 Adet - 1.5"
                    // Son - ile ayrılan fiyat kısmını alalım.

                    var kismi = parca.Trim();

                    // Son '-' işaretinden sonra fiyat olmalı
                    int sonTireIndex = kismi.LastIndexOf('-');
                    if (sonTireIndex < 0)
                        continue;

                    string fiyatStr = kismi.Substring(sonTireIndex + 1).Trim();

                    // Fiyatı decimal'e çevirmeye çalış
                    if (decimal.TryParse(fiyatStr.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal fiyat))
                    {
                        toplam += fiyat;
                    }
                }

                satis.ToplamTutar = toplam;
            }

            db.SaveChanges();
        }
    }
}
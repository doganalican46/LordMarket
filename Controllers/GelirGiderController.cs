using iTextSharp.text.pdf;
using iTextSharp.text;
using LordMarket.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;


namespace LordMarket.Controllers
{
    public class GelirGiderController : Controller
    {
        LordMarketDBEntities db = new LordMarketDBEntities();

        [Authorize]
        public ActionResult GelirGider()
        {
            if (Session["Role"] == null || Session["Role"].ToString() != "admin") { return RedirectToAction("Login", "Home"); }

            var GelirGider = db.GelirGider.ToList();
            return View(GelirGider);
        }

        [Authorize]
        [HttpGet]
        public ActionResult YeniGelirGider()
        {
            return View();
        }

        [Authorize]
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

        [Authorize]
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

        [Authorize]
        public ActionResult GelirGiderGetir(int id)
        {
            var GelirGider = db.GelirGider.Find(id);
            if (GelirGider == null) return HttpNotFound();

            return View(GelirGider);
        }

        [Authorize]
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

        public class RaporViewModel
        {
            public List<SatisIslem> Satislar { get; set; }
            public decimal ToplamTutar { get; set; }
            public DateTime? BaslangicTarihi { get; set; }
            public DateTime? BitisTarihi { get; set; }
        }


        [Authorize]
        [HttpGet]
        public ActionResult Raporlar()
        {
            UpdateSatisToplamTutar();

            return View();
        }

        [Authorize]
        [HttpPost]
        public ActionResult Raporlar(DateTime? baslangicTarihi, DateTime? bitisTarihi)
        {
            UpdateSatisToplamTutar();

            var rapor = new RaporViewModel();

            if (baslangicTarihi.HasValue && bitisTarihi.HasValue)
            {
                rapor.Satislar = db.SatisIslem
                    .Where(s => s.Tarih >= baslangicTarihi && s.Tarih <= bitisTarihi)
                    .OrderBy(s => s.Tarih)
                    .ToList();

                rapor.ToplamTutar = rapor.Satislar.Sum(s => s.ToplamTutar ?? 0);
                rapor.BaslangicTarihi = baslangicTarihi;
                rapor.BitisTarihi = bitisTarihi;
            }
            else
            {
                rapor.Satislar = new List<SatisIslem>();
                rapor.ToplamTutar = 0;
            }

            return View(rapor);
        }

        [Authorize]
        public ActionResult RaporPaneli(DateTime? baslangicTarihi, DateTime? bitisTarihi)
        {
            UpdateSatisToplamTutar();

            if (!baslangicTarihi.HasValue || !bitisTarihi.HasValue)
            {
                baslangicTarihi = DateTime.Today;
                bitisTarihi = DateTime.Today.AddDays(+1);
            }

           

            var satislar = db.SatisIslem
                .Where(x => (!baslangicTarihi.HasValue || x.Tarih >= baslangicTarihi) &&
                            (!bitisTarihi.HasValue || x.Tarih <= bitisTarihi) && x.Status == true)
                .ToList();

            var gelirGiderler = db.GelirGider
                .Where(g => (!baslangicTarihi.HasValue || g.Tarih >= baslangicTarihi) &&
                            (!bitisTarihi.HasValue || g.Tarih <= bitisTarihi) && g.Status == true)
                .ToList();

            ViewBag.Baslangic = baslangicTarihi;
            ViewBag.Bitis = bitisTarihi;

            // Satışlar
            var nakitSatis = satislar.Where(s => s.OdemeTipi == "Nakit").Sum(s => s.ToplamTutar);
            var kartSatis = satislar.Where(s => s.OdemeTipi == "Kart").Sum(s => s.ToplamTutar);
            var veresiyeSatis = satislar.Where(s => s.OdemeTipi == "Veresiye").Sum(s => s.ToplamTutar);

            ViewBag.NakitToplam = nakitSatis;
            ViewBag.KartToplam = kartSatis;
            ViewBag.VeresiyeToplam = veresiyeSatis;

            // Giderler
            var nakitGider = gelirGiderler.Where(g => g.Tur == "NakitGider").Sum(g => g.Tutar);
            var kartGider = gelirGiderler.Where(g => g.Tur == "KartGider").Sum(g => g.Tutar);
            var kasaGider = gelirGiderler.Where(g => g.Tur == "KasaGider").Sum(g => g.Tutar);
            var digerGider = gelirGiderler.Where(g => g.Tur == "Diger").Sum(g => g.Tutar);

            ViewBag.ToplamNakitGider = nakitGider;
            ViewBag.ToplamKartGider = kartGider;
            ViewBag.ToplamKasaGider = kasaGider;
            ViewBag.ToplamDigerGider = digerGider;

            var toplamGelir = nakitSatis + kartSatis;
            var toplamGider = nakitGider + kartGider + kasaGider + digerGider;

            ViewBag.ToplamGelir = toplamGelir;
            ViewBag.ToplamGider = toplamGider;
            ViewBag.NetKazanc = toplamGelir - toplamGider;


            // Günlük Kazanç = nakit + kart satış
            ViewBag.GunlukKazanc = nakitSatis + kartSatis;

            // Nakit Gider + Kart Gider
            ViewBag.ToplamHarcananPara = nakitGider + kartGider + kasaGider + digerGider;

            var kasadaOlmasiGereken = (nakitSatis) - kasaGider;
            ViewBag.KasadaOlmasiGereken = kasadaOlmasiGereken;

            // Kar (Kasadaki Paranın %15'i)
            ViewBag.TahminiKar = kasadaOlmasiGereken * 0.15m;

            // Günlük Toplam Kazanç (gün sonu): Nakit + Kart satış
            ViewBag.GunSonuToplam = nakitSatis + kartSatis;

            // Toplam Gider = nakit + kart gider + 
            ViewBag.ToplamGiderAna = nakitGider + kartGider + kasaGider + digerGider;

            // Günlük Kalan = Gider - (sadece nakit gelir)
            ViewBag.GunlukKalan = (nakitGider + kartGider) - nakitSatis;

            return View(satislar);
        }






        [Authorize]
        public ActionResult SatisIslemGetir(int id)
        {
            var satisIslem = db.SatisIslem.Find(id);

            if (satisIslem == null)
                return HttpNotFound();

            if (satisIslem.MusteriID != null)
            {
                var musteri = db.Musteriler.FirstOrDefault(m => m.ID == satisIslem.MusteriID);
                ViewBag.MusteriAdSoyad = musteri?.MusteriAdSoyad ?? "Bilinmiyor";
            }
            else
            {
                ViewBag.MusteriAdSoyad = "Müşteri kaydı yok";
            }


            return View(satisIslem);
        }



        [Authorize]
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



        

            [Authorize]
        public ActionResult SatisIslemSil(int id)
        {
            var satis = db.SatisIslem.Find(id);
            if (satis != null)
            {
                db.SatisIslem.Remove(satis);
                db.SaveChanges();
            }
            return RedirectToAction("RaporPaneli");
        }



    }
}
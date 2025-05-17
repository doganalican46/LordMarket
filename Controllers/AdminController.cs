using LordMarket.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace LordMarket.Controllers
{
    public class AdminController : Controller
    {
        private LordMarketDBEntities db = new LordMarketDBEntities(); // DbContext veya EF nesneniz

        // GET: Admin
        public ActionResult Index()
        {
            var satislar = db.SatisIslem.Where(s => s.Status == true).ToList();
            var musteriler = db.Musteriler.Where(m => m.Status == true).ToList();
            var gelirGiderler = db.GelirGider.Where(g => g.Status == true).ToList();

            var bugun = DateTime.Today;

            // Günün toplam satış tutarı
            var gununToplamSatis = satislar
                .Where(s => s.Tarih.HasValue && s.Tarih.Value.Date == bugun)
                .Sum(s => (decimal?)s.ToplamTutar) ?? 0;
            ViewBag.GununToplamSatis = gununToplamSatis;

            // Tüm zamanlar gelir ve gider toplamları
            var toplamGelir = gelirGiderler
                .Where(g => g.Tur == "Gelir")
                .Sum(g => (decimal?)g.Tutar) ?? 0;
            var toplamGider = gelirGiderler
                .Where(g => g.Tur == "Gider")
                .Sum(g => (decimal?)g.Tutar) ?? 0;
            ViewBag.ToplamGelir = toplamGelir;
            ViewBag.ToplamGider = toplamGider;

            // Günün gelirleri ve giderleri (tarih filtresi eklendi)
            var gununGelirleri = gelirGiderler
                .Where(g => g.Tur == "Gelir" && g.Tarih.HasValue && g.Tarih.Value.Date == bugun)
                .Sum(g => (decimal?)g.Tutar) ?? 0;
            var gununGiderleri = gelirGiderler
                .Where(g => g.Tur == "Gider" && g.Tarih.HasValue && g.Tarih.Value.Date == bugun)
                .Sum(g => (decimal?)g.Tutar) ?? 0;
            ViewBag.GununToplamGelir = gununGelirleri;
            ViewBag.GununToplamGider = gununGiderleri;

            // Ödeme tiplerine göre toplamlar
            ViewBag.NakitToplam = satislar.Where(s => s.OdemeTipi == "Nakit").Sum(s => (decimal?)s.ToplamTutar) ?? 0;
            ViewBag.KartToplam = satislar.Where(s => s.OdemeTipi == "Kart").Sum(s => (decimal?)s.ToplamTutar) ?? 0;
            ViewBag.VeresiyeToplam = satislar.Where(s => s.OdemeTipi == "Veresiye").Sum(s => (decimal?)s.ToplamTutar) ?? 0;

            // En çok veresiye alan müşteri
            var veresiyeSatislar = satislar.Where(s => s.OdemeTipi == "Veresiye");

            var enCokVeresiyeAlanMusteriID = veresiyeSatislar
                .GroupBy(s => s.MusteriID)
                .OrderByDescending(g => g.Sum(x => x.ToplamTutar))
                .Select(g => g.Key)
                .FirstOrDefault();

            var enCokVeresiyeAlanMusteri = musteriler
                .FirstOrDefault(m => m.ID == enCokVeresiyeAlanMusteriID);

            ViewBag.EnCokVeresiyeAlanMusteri = enCokVeresiyeAlanMusteri != null ? enCokVeresiyeAlanMusteri.MusteriAdSoyad : "Veresiye alan müşteri yok";
            ViewBag.EnCokVeresiyeAlanMusteriID = enCokVeresiyeAlanMusteriID;

            var enCokVeresiyeAlanMusteriBorcu = veresiyeSatislar
                .Where(s => s.MusteriID == enCokVeresiyeAlanMusteriID)
                .Sum(s => s.ToplamTutar);

            ViewBag.EnCokVeresiyeAlanMusteriBorcu = enCokVeresiyeAlanMusteriBorcu;

            return View();
        }



    }
}
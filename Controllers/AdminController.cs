using LordMarket.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;

namespace LordMarket.Controllers
{
    public class AdminController : Controller
    {
        private LordMarketDBEntities db = new LordMarketDBEntities(); // DbContext veya EF nesneniz

        // GET: Admin
        [Authorize]
        public ActionResult Index()
        {
            string userEmail = User.Identity.Name;
            ViewBag.UserEmail = userEmail;
            
            var satislar = db.SatisIslem.Where(s => s.Status == true).ToList();
            var musteriler = db.Musteriler.Where(m => m.Status == true).ToList();
            var gelirGiderler = db.GelirGider.Where(g => g.Status == true).ToList();

            var bugun = DateTime.Today;
            var yarin = bugun.AddDays(1);

            // Ortalama Günlük Satış Sayısı
            var gunlukSatisGruplari = satislar
                .Where(s => s.Tarih.HasValue)
                .GroupBy(s => s.Tarih.Value.Date)
                .Select(g => g.Count())
                .ToList();

            var OrtalamaGunlukSatisSayisi = gunlukSatisGruplari.Any() ? (int)gunlukSatisGruplari.Average() : 0;
            ViewBag.OrtalamaGunlukSatisSayisi = OrtalamaGunlukSatisSayisi;

            // Günün En Yoğun Saat Aralığı (örnek: 14:00-15:00 gibi)
            var bugunkuSatislar = satislar
                .Where(s => s.Tarih.HasValue && s.Tarih.Value.Date == bugun)
                .ToList();

            var saatGruplari = bugunkuSatislar
                .GroupBy(s => s.Tarih.Value.Hour)
                .Select(g => new { Saat = g.Key, Sayi = g.Count() })
                .OrderByDescending(g => g.Sayi)
                .ToList();

            string GununEnYogunSaatAraligi = saatGruplari.Any()
                ? $"{saatGruplari.First().Saat:00}:00 - {saatGruplari.First().Saat + 1:00}:00"
                : "Veri Yok";

            // Genel En Yoğun Saat Aralığı
            var saatGruplariGenel = satislar
                .Where(s => s.Tarih.HasValue)
                .GroupBy(s => s.Tarih.Value.Hour)
                .Select(g => new { Saat = g.Key, Sayi = g.Count() })
                .OrderByDescending(g => g.Sayi)
                .ToList();

            string GenelEnYogunSaatAraligi = saatGruplariGenel.Any()
                ? $"{saatGruplariGenel.First().Saat:00}:00 - {saatGruplariGenel.First().Saat + 1:00}:00"
                : "Veri Yok";

            ViewBag.GenelEnYogunSaatAraligi = GenelEnYogunSaatAraligi;

            ViewBag.GununEnYogunSaatAraligi = GununEnYogunSaatAraligi;

            var urunSayilari = new Dictionary<string, int>();

            foreach (var satis in satislar)
            {
                if (string.IsNullOrEmpty(satis.UrunListesi)) continue;

                var satirlar = satis.UrunListesi.Split('\n');

                foreach (var satir in satirlar)
                {
                    if (string.IsNullOrWhiteSpace(satir)) continue;

                    var urunParcala = satir.Split('-');

                    if (urunParcala.Length >= 2)
                    {
                        var urunAdi = urunParcala[0].Replace("Ürün:", "").Trim();
                        var adetStr = urunParcala[1].Replace("Adet:", "").Trim();

                        if (int.TryParse(adetStr, out int adet))
                        {
                            if (urunSayilari.ContainsKey(urunAdi))
                                urunSayilari[urunAdi] += adet;
                            else
                                urunSayilari[urunAdi] = adet;
                        }
                    }
                }
            }

            var enCokSatilanUrunler = urunSayilari
                .OrderByDescending(x => x.Value)
                .Take(10)
                .ToList();

            ViewBag.EnCokSatilanUrunler = enCokSatilanUrunler;


            // Günün toplam satış tutarı
            var gununToplamSatis = satislar
                                    .Where(s => s.Tarih.HasValue
                                                && s.Tarih.Value.Date == bugun
                                                && (s.OdemeTipi == "Nakit" || s.OdemeTipi == "Kart"))
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
    .Where(g => g.Tur == "Gelir" && g.Tarih >= bugun && g.Tarih < bugun.AddDays(1))
    .Sum(g => (decimal?)g.Tutar) ?? 0;

            var gununKartGiderleri = gelirGiderler
                .Where(g => g.Tur == "KartGider" && g.Tarih >= bugun && g.Tarih < bugun.AddDays(1))
                .Sum(g => (decimal?)g.Tutar) ?? 0;
            var gununNakitGiderleri = gelirGiderler
                .Where(g => g.Tur == "NakitGider" && g.Tarih >= bugun && g.Tarih < bugun.AddDays(1))
                .Sum(g => (decimal?)g.Tutar) ?? 0;
            var gununKasaGider = gelirGiderler
                .Where(g => g.Tur == "KasaGider" && g.Tarih >= bugun && g.Tarih < bugun.AddDays(1))
                .Sum(g => (decimal?)g.Tutar) ?? 0;

            ViewBag.GununToplamGelir = gununGelirleri;
            ViewBag.gununKartGiderleri = gununKartGiderleri;
            ViewBag.gununNakitGiderleri = gununNakitGiderleri;
            ViewBag.gununKasaGider = gununKasaGider;

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

        [Authorize]
        public ActionResult UserProfile()
        {
            if (Session["ID"] == null)
            {
                return RedirectToAction("Login", "Home");
            }

            int userId = (int)Session["ID"];
            var user = db.Kullanicilar.FirstOrDefault(x => x.ID == userId);

            if (user == null)
            {
                return HttpNotFound();
            }

            if (Session["Username"] != null)
            {
                user.Username = Session["Username"].ToString();
            }
            if (Session["Mail"] != null)
            {
                user.Mail = Session["Mail"].ToString();
            }
            if (Session["Image"] != null)
            {
                user.Image = Session["Image"].ToString();
            }

            return View(user);
        }


        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateUser(int id, Kullanicilar user, HttpPostedFileBase Image)
        {
            //if (Image != null && Image.ContentLength > 0)
            //{
            //    string filePath = Path.Combine(Server.MapPath("~/Images/Users"), Path.GetFileName(Image.FileName));
            //    Image.SaveAs(filePath);
            //    user.Image = "/Images/Users/" + Path.GetFileName(Image.FileName);
            //}

            var existingUser = db.Kullanicilar.FirstOrDefault(x => x.ID == id);
            if (existingUser != null)
            {
                existingUser.Username = user.Username;
                existingUser.Mail = user.Mail;
                existingUser.Password = user.Password;
                existingUser.SonGuncellenmeTarihi =DateTime.Now.ToString();

                db.SaveChanges();

                Session["Username"] = user.Username;
                Session["Mail"] = user.Mail;
                Session["Image"] = user.Image;
                Session["Password"] = user.Image;

                return RedirectToAction("UserProfile");
            }

            return RedirectToAction("Index", "Home");
        }


        public ActionResult Logout()
        {
            Session.Clear();
            FormsAuthentication.SignOut();
            return RedirectToAction("Index", "Home");
        }


        [Authorize]
        public ActionResult ResetSatisIslem()
        {
            // 1. ToplamTutar'ları gruplara göre al
            decimal toplamNakitTutar = db.SatisIslem
                .Where(s => s.Status == true && s.OdemeTipi == "Nakit")
                .Sum(s => (decimal?)s.ToplamTutar) ?? 0;

            decimal toplamKartTutar = db.SatisIslem
                .Where(s => s.Status == true && s.OdemeTipi == "Kart")
                .Sum(s => (decimal?)s.ToplamTutar) ?? 0;

            decimal toplamVeresiyeTutar = db.SatisIslem
                .Where(s => s.Status == true && s.OdemeTipi == "Veresiye")
                .Sum(s => (decimal?)s.ToplamTutar) ?? 0;

            // 2. Tablodaki tüm verileri sil
            var tumSatislar = db.SatisIslem.ToList();
            db.SatisIslem.RemoveRange(tumSatislar);
            db.SaveChanges();

            // 3. Yeni özet kayıtları oluştur
            if (toplamNakitTutar > 0)
            {
                db.SatisIslem.Add(new SatisIslem
                {
                    OdemeTipi = "Nakit",
                    ToplamTutar = toplamNakitTutar,
                    Status = true,
                    BosAlan = "SatisIslem tablosu temizlendi.",
                    Tarih = DateTime.Now.AddDays(-1)
                });
            }

            if (toplamKartTutar > 0)
            {
                db.SatisIslem.Add(new SatisIslem
                {
                    OdemeTipi = "Kart",
                    ToplamTutar = toplamKartTutar,
                    Status = true,
                    BosAlan = "SatisIslem tablosu temizlendi.",
                    Tarih = DateTime.Now.AddDays(-1)
                });
            }

            if (toplamVeresiyeTutar > 0)
            {
                db.SatisIslem.Add(new SatisIslem
                {
                    OdemeTipi = "Veresiye",
                    ToplamTutar = toplamVeresiyeTutar,
                    Status = true,
                    BosAlan = "SatisIslem tablosu temizlendi.",
                    Tarih = DateTime.Now.AddDays(-1)
                });
            }

            db.SaveChanges();

            TempData["Mesaj"] = "SatisIslem tablosu temizlendi ve özet kayıtlar eklendi.";
            return RedirectToAction("SatisGecmis", "GelirGider");
        }




    }
}
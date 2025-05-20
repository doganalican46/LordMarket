using LordMarket.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;

namespace LordMarket.Controllers
{
    public class HomeController : Controller
    {
        LordMarketDBEntities db = new LordMarketDBEntities();

        public class SatisIslemViewModel1
        {
            public List<GelirGider> GelirGider { get; set; }
            public List<Musteriler> Musteriler { get; set; }
            public List<SatisIslem> SatisIslem { get; set; }
            public List<Urunler> Urunler { get; set; }
            public List<Urunler> HizliUrunler { get; set; }

            public SatisIslem SonSatis { get; set; }
            public string MusteriAdSoyad { get; set; }
        }





        public ActionResult Index()
        {
            UpdateSatisToplamTutar();

            var satisListesi = db.SatisIslem.Where(h => h.Status == true).OrderByDescending(x => x.ID).ToList();
            var sonSatis = satisListesi.FirstOrDefault();

            string musteriAdSoyad = null;

            if (sonSatis?.MusteriID != null)
            {
                var musteri = db.Musteriler.FirstOrDefault(x => x.ID == sonSatis.MusteriID);
                musteriAdSoyad = musteri != null ? musteri.MusteriAdSoyad : null;
            }

            var viewModel = new SatisIslemViewModel1
            {
                GelirGider = db.GelirGider.Where(h => h.Status == true).ToList(),
                Musteriler = db.Musteriler.Where(h => h.Status == true).ToList(),
                SatisIslem = satisListesi,
                Urunler = db.Urunler.Where(h => h.Status == true).ToList(),
                HizliUrunler = db.Urunler.Where(h => h.Status == true && h.HizliUrunMu == true).ToList(),
                SonSatis = sonSatis,
                MusteriAdSoyad = musteriAdSoyad
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
        public JsonResult SatisYap(string OdemeTipi, string UrunListesi, int? MusteriID)
        {
            try
            {
                if (OdemeTipi == "Veresiye" && (MusteriID == null || MusteriID == 0))
                {
                    return Json(new { success = false, message = "Veresiye satış için müşteri seçimi zorunludur." });
                }

                if (string.IsNullOrWhiteSpace(UrunListesi))
                {
                    return Json(new { success = false, message = "Ürün listesi boş olamaz." });
                }

                Musteriler musteri = null;
                decimal toplam = 0;
                StringBuilder yeniNot = new StringBuilder();
                string tarih = DateTime.Now.ToString("dd.MM.yyyy");

                yeniNot.AppendLine($"[Tarih: {tarih}]");
                string[] urunSatirlari = UrunListesi.Split('\n');

                foreach (var satir in urunSatirlari)
                {
                    if (!string.IsNullOrWhiteSpace(satir))
                    {
                        string[] parcalar = satir.Split('-');
                        if (parcalar.Length == 3)
                        {
                            string urunAd = parcalar[0].Replace("Ürün:", "").Trim();
                            string adetStr = parcalar[1].Replace("Adet:", "").Trim();
                            string fiyatStr = parcalar[2].Replace("Tutar:", "").Replace("₺", "").Trim();

                            bool adetOk = int.TryParse(adetStr, out int adet);
                            bool fiyatOk = decimal.TryParse(fiyatStr, System.Globalization.NumberStyles.Any,
                                System.Globalization.CultureInfo.InvariantCulture, out decimal birimFiyat);

                            if (adetOk && fiyatOk)
                            {
                                decimal satirToplam = adet * birimFiyat;
                                toplam += satirToplam;

                                yeniNot.AppendLine($"Ürün: {urunAd} | Adet: {adet} | Birim: {birimFiyat}₺ | Toplam: {satirToplam}₺");
                            }
                        }
                    }
                }

                yeniNot.AppendLine("-----------------------------");

                if (OdemeTipi == "Veresiye" && MusteriID.HasValue)
                {
                    musteri = db.Musteriler.FirstOrDefault(m => m.ID == MusteriID.Value);
                    if (musteri == null)
                    {
                        return Json(new { success = false, message = "Müşteri bulunamadı." });
                    }

                    musteri.ToplamBorc = (musteri.ToplamBorc ?? 0) + toplam;
                    musteri.Notlar = (musteri.Notlar ?? "") + yeniNot.ToString();
                }

                SatisIslem yeniSatis = new SatisIslem
                {
                    ToplamTutar = toplam,
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

        [HttpPost]
        public ActionResult YeniUrun(Urunler urun)
        {
            if (ModelState.IsValid)
            {
                urun.Status = true;
                urun.EklenmeTarihi = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
                db.Urunler.Add(urun);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(urun);
        }


        [HttpPost]
        public ActionResult HizliUrunYap(string barkod)
        {
            var urun = db.Urunler.FirstOrDefault(x => x.Barkod == barkod);

            urun.HizliUrunMu = true;
            db.SaveChanges();

            return View("Index");
        }



        [HttpGet]
        public ActionResult Login()
        {
            return View();
        }



        [HttpPost]
        public ActionResult Login(Kullanicilar k)
        {
            var logindeger = db.Kullanicilar
                .FirstOrDefault(x =>
                    (x.Mail == k.Username || x.Username == k.Username) &&
                    x.Password == k.Password);

            if (logindeger != null)
            {
                FormsAuthentication.SetAuthCookie(logindeger.Mail, false);

                Session["ID"] = logindeger.ID;
                Session["Username"] = logindeger.Username;
                Session["Mail"] = logindeger.Mail;
                Session["Password"] = logindeger.Password;
                Session["Image"] = logindeger.Image;
                Session["Role"] = logindeger.Role;
                Session["Status"] = logindeger.Status;

                if (logindeger.Role == "admin")
                {
                    return RedirectToAction("Index", "Admin");
                }
                else
                {
                    return RedirectToAction("Index", "Home");
                }
            }
            else
            {
                TempData["DangerMessage"] = "E-posta veya şifreniz hatalı, tekrar deneyiniz.";
                return View();
            }
        }


        [HttpPost]
        public ActionResult HizliUrunKaldir(int id)
        {
            var urun = db.Urunler.FirstOrDefault(u => u.ID == id);
            if (urun != null)
            {
                urun.HizliUrunMu = false;
                db.SaveChanges();
                return Json(new { success = true });
            }

            return Json(new { success = false });
        }

       






    }
}
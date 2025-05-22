using LordMarket.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Net;
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
            var urun = db.Urunler
                         .Where(x => x.Barkod == barkod && x.Status == true)
                         .Select(x => new { x.UrunAd, x.UrunFiyat })
                         .FirstOrDefault();
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

        public JsonResult BarkodAraYeniUrun(string barkod)
        {
            
                var urun = db.Urunler
                    .Where(u => u.Barkod == barkod)
                    .Select(u => new
                    {
                        u.UrunAd,
                        u.UrunKategori,
                        u.UrunAlisFiyati,
                        u.KDVOran,
                        u.UrunFiyat,
                        u.UrunResmi,
                        u.HizliUrunMu
                    })
                    .FirstOrDefault();

                return Json(urun, JsonRequestBehavior.AllowGet);
            
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

        [Authorize]
        [HttpPost]
        public ActionResult YeniUrun(Urunler urun)
        {
            if (ModelState.IsValid)
            {
                var mevcutUrun = db.Urunler.FirstOrDefault(x => x.Barkod == urun.Barkod);

                if (mevcutUrun != null)
                {
                    // Güncelleme işlemi
                    mevcutUrun.UrunAd = urun.UrunAd;
                    mevcutUrun.UrunKategori = urun.UrunKategori;
                    mevcutUrun.UrunAlisFiyati = urun.UrunAlisFiyati;
                    mevcutUrun.KDVOran = urun.KDVOran;
                    mevcutUrun.UrunFiyat = urun.UrunFiyat;
                    mevcutUrun.UrunResmi = urun.UrunResmi;
                    mevcutUrun.HizliUrunMu = urun.HizliUrunMu;
                    mevcutUrun.GuncellenmeTarihi = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
                    mevcutUrun.Status = true; // Güncellendiği için aktif olsun

                    db.SaveChanges();
                }
                else
                {
                    // Yeni ürün olarak ekle
                    urun.Status = true;
                    urun.EklenmeTarihi = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
                    db.Urunler.Add(urun);
                    db.SaveChanges();
                }

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




        [Authorize]
        [HttpGet]
        public JsonResult ToptanciBildirimi()
        {
            string bugun = DateTime.Today.ToString("dddd", new System.Globalization.CultureInfo("tr-TR"));
            var aktifToptancilar = db.Kullanicilar
                .Where(k => k.Role == "toptanci" && k.Status == true)
                .ToList();

            bool bugunGelenToptanciVarMi = aktifToptancilar.Any(k =>
                !string.IsNullOrEmpty(k.Password) &&
                k.Password.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                          .Any(gun => gun.Trim().Equals(bugun, StringComparison.OrdinalIgnoreCase))
            );

            var result = new { mailGonderildi = false, mesaj = "" };

            if (bugunGelenToptanciVarMi)
            {
                // Saat kontrolü 09:00 - 12:00 arası
                var now = DateTime.Now.TimeOfDay;
                var baslangic = new TimeSpan(9, 0, 0);
                var bitis = new TimeSpan(12, 0, 0);

                if (now >= baslangic && now <= bitis)
                {
                    // Mail gönderme sınır kontrolü için session kullanalım
                    int mailSayisi = 0;
                    if (System.Web.HttpContext.Current.Session["mailSayisi"] != null)
                    {
                        mailSayisi = (int)System.Web.HttpContext.Current.Session["mailSayisi"];
                    }

                    if (mailSayisi < 3)
                    {
                        try
                        {
                            var smtpClient = new SmtpClient("smtp.gmail.com")
                            {
                                Port = 587,
                                Credentials = new NetworkCredential("lordtekelbufe@gmail.com", "pofz rmri bmnl odgb"),
                                EnableSsl = true
                            };

                            string mailBody = $@"
<html>
<body style='font-family: Arial, sans-serif; background-color: #f4f4f4; padding: 20px;'>
    <div style='background-color: #ffffff; padding: 30px; border-radius: 10px; box-shadow: 0 2px 10px rgba(0,0,0,0.1);'>
        <h2 style='color: #007bff;'>Loooooooooo!!!! Bugün Gelecek Toptancılar Var Hazırlık yap :) </h2>
        <p><strong>Bugün hangi gün ki? </strong> {bugun}</p>
        <hr />
        <ul style='padding-left: 20px;'>";

                            foreach (var k in aktifToptancilar)
                            {
                                if (!string.IsNullOrEmpty(k.Password) &&
                                    k.Password.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                                              .Any(gun => gun.Trim().Equals(bugun, StringComparison.OrdinalIgnoreCase)))
                                {
                                    mailBody += $@"
            <li style='margin-bottom:10px;'>
                <strong>Toptancı Adı:</strong> {k.Username} <br />
                <strong>Diğer geldiği günler:</strong> {k.Password}
            </li>";
                                }
                            }

                            mailBody += @"
        </ul>
        <hr />
        <footer style='font-size:12px; color:#666; margin-top:20px;'>
            Lord Büfe&Market - <a href='https://doganalican46.dev' target='_blank'>doganalican46.dev</a>
        </footer>
    </div>
</body>
</html>";

                            var mailMessage = new MailMessage
                            {
                                From = new MailAddress("lordtekelbufe@gmail.com"),
                                Subject = $"Toptancı Bildirimi ({bugun}) - Lord Tekel Büfe",
                                Body = mailBody,
                                IsBodyHtml = true
                            };

                            mailMessage.To.Add("cengizack56@gmail.com");
                            smtpClient.Send(mailMessage);

                            // Mail sayısını arttır
                            System.Web.HttpContext.Current.Session["mailSayisi"] = mailSayisi + 1;

                            result = new { mailGonderildi = true, mesaj = "Mail gönderildi." };
                        }
                        catch (Exception ex)
                        {
                            result = new { mailGonderildi = false, mesaj = "Mail gönderim hatası: " + ex.Message };
                        }
                    }
                    else
                    {
                        result = new { mailGonderildi = false, mesaj = "Mail gönderme sınırı aşıldı." };
                    }
                }
                else
                {
                    result = new { mailGonderildi = false, mesaj = "Mail gönderim saati değil." };
                }
            }
            else
            {
                result = new { mailGonderildi = false, mesaj = "Bugün gelen toptancı yok." };
            }

            return Json(result, JsonRequestBehavior.AllowGet);
        }



    }
}
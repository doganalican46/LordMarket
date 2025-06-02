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

        private static List<Urunler> urunCache;

        private void UrunleriCachedenYukle()
        {
            if (urunCache == null || !urunCache.Any())
            {
                urunCache = db.Urunler.Where(x => x.Status == true).ToList();
            }
        }

        public static void UrunCacheYenile()
        {
            
            urunCache = null;
        }



        public ActionResult Error()
        {
            return View(); 
        }


        public ActionResult Index()
        {
            UpdateSatisToplamTutar();
            UrunCacheYenile();
            UrunleriCachedenYukle();

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
                MusteriAdSoyad = musteriAdSoyad,

            };

            return View(viewModel);
        }





        [HttpPost]
        public JsonResult BarkodAra(string barkod)
        {
            UrunleriCachedenYukle(); 

            var urun = urunCache
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

                // Urun listesinde iskonto var mı diye kontrol et
                bool iskontoVar = urunSatirlari.Any(satir => satir.Contains("İskonto"));

                if (iskontoVar)
                {
                    toplam = 0; // İskonto varsa toplam sıfır olur
                }
                else
                {
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

                return Json(new { success = true, beklemede = OdemeTipi == "Beklemede" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Hata oluştu: " + ex.Message });
            }
        }







        private void UpdateSatisToplamTutar()
        {
            var satislar = db.SatisIslem
                .Where((s => s.Status == true && (!s.ToplamTutar.HasValue || s.ToplamTutar == 0)))
                .ToList();

            foreach (var satis in satislar)
            {
                if (string.IsNullOrWhiteSpace(satis.UrunListesi))
                    continue;

                decimal toplam = 0;
                decimal iskontoOrani = 0; // yüzde cinsinden, örn: 10 için 10

                var urunSatirlari = satis.UrunListesi
                    .Split(new[] { "Ürün:" }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var satir in urunSatirlari)
                {
                    if (satir.Contains("İskonto"))
                    {
                        // Satırdan %10 gibi ifadeyi yakalayalım
                        var yüzdeIndex = satir.IndexOf('%');
                        if (yüzdeIndex > 0)
                        {
                            // % işaretinden önceki iki haneli oranı alma mantığı
                            // Satırın başında " İskonto (%10) - ..." var
                            // Parantez içini almak için önce '(' ve ')' indekslerini bul
                            int parantezAcIndex = satir.IndexOf('(');
                            int parantezKapaIndex = satir.IndexOf(')');
                            if (parantezAcIndex >= 0 && parantezKapaIndex > parantezAcIndex)
                            {
                                string oranStr = satir.Substring(parantezAcIndex + 1, parantezKapaIndex - parantezAcIndex - 1);
                                // oranStr = "%10"
                                oranStr = oranStr.Replace("%", "").Trim();
                                if (decimal.TryParse(oranStr, out decimal oran))
                                {
                                    iskontoOrani = oran;
                                }
                            }
                        }
                    }
                    else
                    {
                        // İskonto olmayan satırlardaki tutarları topla
                        var tutarIndex = satir.IndexOf("Tutar:");
                        if (tutarIndex == -1)
                            continue;

                        var tutarStr = satir.Substring(tutarIndex + "Tutar:".Length).Trim();

                        if (decimal.TryParse(tutarStr.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal tutar))
                        {
                            toplam += tutar;
                        }
                    }
                }

                // İskonto varsa toplamdan düş
                if (iskontoOrani > 0)
                {
                    var indirimMiktari = toplam * (iskontoOrani / 100);
                    toplam -= indirimMiktari;
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
                UrunCacheYenile();

                return RedirectToAction("Index");
            }
            UrunCacheYenile();

            return View(urun);
        }



        [HttpPost]
        public ActionResult HizliUrunYap(string barkod)
        {
            var urun = db.Urunler.FirstOrDefault(x => x.Barkod == barkod);

            urun.HizliUrunMu = true;
            db.SaveChanges();
            UrunCacheYenile();

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
                UrunCacheYenile();

                return Json(new { success = true });
            }
            UrunCacheYenile();

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



        [HttpPost]
        public JsonResult SatisIslemBeklet(string urunListesi)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(urunListesi))
                {
                    return Json(new { success = false, message = "Ürün listesi boş olamaz." });
                }

                SatisIslem bekleyenSatis = new SatisIslem
                {
                    UrunListesi = urunListesi,
                    OdemeTipi = "beklemede",
                    Tarih = DateTime.Now,
                    Status = true
                };

                db.SatisIslem.Add(bekleyenSatis);
                db.SaveChanges();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Hata: " + ex.Message });
            }
        }


        [HttpPost]
        public JsonResult BeklemedeSatisGetir()
        {
            var bekleyen = db.SatisIslem
                             .Where(x => x.OdemeTipi == "Beklemede" && x.Status==true)
                             .OrderByDescending(x => x.Tarih)
                             .FirstOrDefault();

            if (bekleyen == null)
            {
                return Json(new { success = false, message = "Beklemede işlem bulunamadı." });
            }

            // Status'u false yap
            bekleyen.Status = false;
            db.SaveChanges();

            return Json(new { success = true, urunListesi = bekleyen.UrunListesi });
        }



        [HttpGet]
        public JsonResult BeklemedeIslemVarMi()
        {
            var onDakikaOnce = DateTime.Now.AddMinutes(-10);

            var varMi = db.SatisIslem
                          .Any(x => x.OdemeTipi == "Beklemede" && x.Status == true && x.Tarih >= onDakikaOnce);

            if (!varMi)
            {
                var bekleyenIslemler = db.SatisIslem
                                         .Where(x => x.OdemeTipi == "Beklemede" && x.Status == true)
                                         .ToList();

                foreach (var islem in bekleyenIslemler)
                {
                    islem.Status = false;
                }

                db.SaveChanges();
            }

            return Json(new { varMi = varMi }, JsonRequestBehavior.AllowGet);
        }



        [HttpGet]
        public JsonResult GetMusteriBorc(int id)
        {
            var musteri = db.Musteriler.Find(id);
            if (musteri != null)
            {
                return Json(musteri.ToplamBorc, JsonRequestBehavior.AllowGet);
            }
            return Json(0, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult BorcOde(int id, decimal OdenenBorcTutar, string Not, string OdemeTipi)
        {
            var musteri = db.Musteriler.Find(id);
            if (musteri == null) return HttpNotFound();

            if(OdemeTipi== "RaporaYansitma")
            {
                musteri.ToplamBorc -= OdenenBorcTutar;
                string bilgi2 = $"Borç Ödemesi: {DateTime.Now:yyyy-MM-dd HH:mm} - {OdenenBorcTutar} ₺ Not: {Not} Ödeme Tipi: {OdemeTipi} ||";
                musteri.BosAlan += bilgi2;
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            musteri.ToplamBorc -= OdenenBorcTutar;

            string bilgi = $"Borç Ödemesi: {DateTime.Now:yyyy-MM-dd HH:mm} - {OdenenBorcTutar} ₺ Not: {Not} Ödeme Tipi: {OdemeTipi} ||";
            musteri.BosAlan += bilgi;

            // Yeni SatisIslem kaydı
            var islem = new SatisIslem
            {
                MusteriID = id,
                ToplamTutar = OdenenBorcTutar,
                OdemeTipi = OdemeTipi,
                Tarih = DateTime.Now,
                UrunListesi = $"Ürün: Veresiye borcu ödendi - Adet: 1 - Tutar: {OdenenBorcTutar} ₺",
                Status = true
            };
            db.SatisIslem.Add(islem);

            db.SaveChanges();
            return RedirectToAction("Index");
        }





    }
}
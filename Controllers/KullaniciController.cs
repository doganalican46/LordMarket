using LordMarket.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Net;
using System.Web;
using System.Web.Mvc;

namespace LordMarket.Controllers
{
    public class KullaniciController : Controller
    {
        LordMarketDBEntities db = new LordMarketDBEntities();

        [Authorize]
        public ActionResult Kullanicilar()
        {
            var Kullanicilar = db.Kullanicilar.ToList();
            return View(Kullanicilar);
        }

        [Authorize]
        [HttpGet]
        public ActionResult YeniKullanici()
        {
            return View();
        }

        [Authorize]
        [HttpPost]
        public ActionResult YeniKullanici(Kullanicilar Kullanici)
        {
            if (ModelState.IsValid)
            {
                Kullanici.Status = true;
                Kullanici.SonGuncellenmeTarihi = DateTime.Now.ToString();
                db.Kullanicilar.Add(Kullanici);
                db.SaveChanges();
                return RedirectToAction("Kullanicilar");
            }

            return View(Kullanici);
        }

        [Authorize]
        public ActionResult KullaniciSil(int id)
        {
            var Kullanici = db.Kullanicilar.Find(id);
            if (Kullanici != null)
            {
                Kullanici.Status = false;
                db.SaveChanges();
            }
            return RedirectToAction("Kullanicilar");
        }

        [Authorize]
        public ActionResult KullaniciGetir(int id)
        {
            var Kullanici = db.Kullanicilar.Find(id);
            if (Kullanici == null) return HttpNotFound();

            return View(Kullanici);
        }

        [Authorize]
        [HttpPost]
        public ActionResult KullaniciGuncelle(Kullanicilar y)
        {
            if (ModelState.IsValid)
            {
                var Kullanici = db.Kullanicilar.Find(y.ID);
                if (Kullanici == null) return HttpNotFound();

                Kullanici.Username = y.Username;
                Kullanici.Password = y.Password;
                Kullanici.Image = y.Image;
                Kullanici.Role = y.Role;
                Kullanici.SonGuncellenmeTarihi = DateTime.Now.ToString();
                Kullanici.Status = y.Status;

                db.SaveChanges();
                return RedirectToAction("Kullanicilar");
            }

            return View("KullaniciGetir", y);
        }


        [Authorize]
        public ActionResult Toptancilar()
        {
            var Toptancilar = db.Kullanicilar.Where(x => x.Role == "toptanci").ToList();
            return View(Toptancilar);
        }

        [Authorize]
        [HttpGet]
        public ActionResult YeniToptanci()
        {
            return View();
        }

        [Authorize]
        [HttpPost]
        public ActionResult YeniToptanci(Kullanicilar Kullanici)
        {
            if (ModelState.IsValid)
            {
                Kullanici.Status = true;
                Kullanici.Role = "toptanci";
                
                Kullanici.SonGuncellenmeTarihi = DateTime.Now.ToString();
                db.Kullanicilar.Add(Kullanici);
                db.SaveChanges();
                return RedirectToAction("Toptancilar");
            }

            return View(Kullanici);
        }

        [Authorize]
        public ActionResult ToptanciSil(int id)
        {
            var Kullanici = db.Kullanicilar.Find(id);
            if (Kullanici != null)
            {
                Kullanici.Status = false;
                db.SaveChanges();
            }
            return RedirectToAction("Toptancilar");
        }

        [Authorize]
        public ActionResult ToptanciKaldir(int id)
        {
            var toptanci = db.Kullanicilar.Find(id);
            if (toptanci != null)
            {
                db.Kullanicilar.Remove(toptanci);
                db.SaveChanges();
            }
            return RedirectToAction("Toptancilar");
        }

        [Authorize]
        public ActionResult ToptanciGetir(int id)
        {
            var Kullanici = db.Kullanicilar.Find(id);
            if (Kullanici == null) return HttpNotFound();

            return View(Kullanici);
        }

        [Authorize]
        [HttpPost]
        public ActionResult ToptanciGuncelle(Kullanicilar y)
        {
            if (ModelState.IsValid)
            {
                var Kullanici = db.Kullanicilar.Find(y.ID);
                if (Kullanici == null) return HttpNotFound();

                Kullanici.Username = y.Username;
                Kullanici.Password = y.Password;
                Kullanici.Image = y.Image;
                Kullanici.SonGuncellenmeTarihi = DateTime.Now.ToString();
                Kullanici.Status = y.Status;

                db.SaveChanges();
                return RedirectToAction("Toptancilar");
            }

            return View("ToptanciGetir", y);
        }



        //pofz rmri bmnl odgb

        [Authorize]
        [HttpGet]
        public ActionResult ToptanciBildirimi()
        {
            // =================== 🔍 Bugünkü Gün Tespiti ===================
            string bugun = DateTime.Today.ToString("dddd", new System.Globalization.CultureInfo("tr-TR"));
            // Örn: Pazartesi

            // =================== 🔍 Aktif Toptancıları Getir ===================
            var aktifToptancilar = db.Kullanicilar
                .Where(k => k.Role == "toptanci" && k.Status == true)
                .ToList();

            bool bugunGelenToptanciVarMi = aktifToptancilar.Any(k =>
                !string.IsNullOrEmpty(k.Password) &&
                k.Password.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                          .Any(gun => gun.Trim().Equals(bugun, StringComparison.OrdinalIgnoreCase))
            );

            // =================== ✅ Bugün gelen varsa mail gönder ===================
            if (bugunGelenToptanciVarMi)
            {
                try
                {
                    var smtpClient = new SmtpClient("smtp.gmail.com")
                    {
                        Port = 587,
                        Credentials = new NetworkCredential("lordtekelbufe@gmail.com", "pofz rmri bmnl odgb"),
                        EnableSsl = true
                    };

                    // Mail body içeriğini dinamik olarak oluştur
                    string mailBody = $@"
<html>
<body style='font-family: Arial, sans-serif; background-color: #f4f4f4; padding: 20px;'>
    <div style='background-color: #ffffff; padding: 30px; border-radius: 10px; box-shadow: 0 2px 10px rgba(0,0,0,0.1);'>
        <h2 style='color: #007bff;'>📦 Lord Büfe & Market - Bugün Gelecek Toptancılar</h2>
        <p><strong>Bugün hangi gün ki? </strong> {bugun}</p>
        <hr />
        <ul style='padding-left: 20px;'>";

                    // Bugün gelen toptancıları tek tek ekle
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

                    mailBody += $@"
        </ul>
        <hr />
        <p style='margin-top:30px; font-size:14px; color:#555;'>Bu bildirim <strong>Lord Büfe & Market</strong> sisteminden otomatik olarak gönderilmiştir.</p>
        <p style='font-size:13px; color:#777;'>Yazılım geliştiriciniz: <a href='https://www.doganalican46.dev' target='_blank' style='color:#007bff; text-decoration:none;'>www.doganalican46.dev</a></p>
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

                    mailMessage.To.Add("alicanalican4141@gmail.com");
                    smtpClient.Send(mailMessage);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Mail gönderim hatası: " + ex.Message);
                }
            }

            return RedirectToAction("Toptancilar");
        }







    }
}
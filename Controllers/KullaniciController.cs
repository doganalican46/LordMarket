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
            var Kullanicilar = db.Kullanicilar.Where(x=>x.Role=="admin"|| x.Role== "user").ToList();
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
            var toptancilar = db.Kullanicilar.Where(x => x.Role == "Toptanci").ToList();

            // ViewBag içine net durum sözlüğü atalım
            var durumlar = new Dictionary<int, (decimal Alacak, decimal Verecek, decimal Net)>();

            foreach (var t in toptancilar)
            {
                var alacak = db.GelirGider
                               .Where(x => x.Tur == "Alacak" && x.ToptanciID == t.ID && x.Status == true)
                               .Sum(x => (decimal?)x.Tutar) ?? 0;

                var verecek = db.GelirGider
                                .Where(x => x.Tur == "Verecek" && x.ToptanciID == t.ID && x.Status == true)
                                .Sum(x => (decimal?)x.Tutar) ?? 0;

                var net = alacak - verecek;
                durumlar[t.ID] = (alacak, verecek, net);
            }

            ViewBag.ToptanciDurumlar = durumlar;

            return View(toptancilar);
        }

        [HttpPost]
        public ActionResult ToptanciIslem(int id, decimal IslemTutar, string Not, string IslemTipi)
        {
            var toptanci = db.Kullanicilar.FirstOrDefault(x => x.ID == id && x.Role == "toptanci");
            if (toptanci == null) return HttpNotFound();

            // Yeni GelirGider kaydı
            var gelirGider = new GelirGider
            {
                Tur = IslemTipi, // "Alacak" veya "Verecek"
                Tutar = IslemTutar,
                Notlar = Not,
                Tarih = DateTime.Now,
                Status = true,
                BosAlan = $"Toptancı İşlem: {toptanci.Username} - {DateTime.Now:yyyy-MM-dd HH:mm}",
                ToptanciID = toptanci.ID // burayı GelirGider modeline eklemiş olman gerekiyor
            };

            db.GelirGider.Add(gelirGider);
            db.SaveChanges();

            return RedirectToAction("Toptancilar");
        }

        [HttpPost]
        public JsonResult ResetToptanciIslemleri(int id)
        {
            try
            {
                var islemler = db.GelirGider.Where(g => g.ToptanciID == id && g.Status == true).ToList();
                foreach (var islem in islemler)
                {
                    islem.Status = false; // pasif yap
                }
                db.SaveChanges();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
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

        









    }
}
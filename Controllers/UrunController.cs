using iTextSharp.text;
using iTextSharp.text.pdf;
using LordMarket.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Web;
using System.Web.Mvc;
using System.Xml.Linq;

namespace LordMarket.Controllers
{
    public class UrunController : Controller
    {
        LordMarketDBEntities db = new LordMarketDBEntities();

        [Authorize]
        public ActionResult Urunler()
        {
            //ViewBag.Kategoriler = GetKategoriSelectList();
            var urunler = db.Urunler.Where(x=>x.Status==true).ToList();
            return View(urunler);
        }

        [Authorize]
        public ActionResult HizliUrunler()
        {
            var urunler = db.Urunler.Where(m=>m.HizliUrunMu==true).ToList();
            return View(urunler);
        }

        [Authorize]
        [HttpGet]
        public ActionResult YeniUrun()
        {
            return View();
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

                return RedirectToAction("Urunler");
            }

            return View(urun);
        }


        [Authorize]
        public ActionResult UrunSil(int id)
        {
            var urun = db.Urunler.Find(id);
            if (urun != null)
            {
                urun.Status = false;
                db.SaveChanges();
            }
            return RedirectToAction("Urunler");
        }

        [Authorize]
        public ActionResult TamamenUrunSil(int id)
        {
            var urun = db.Urunler.Find(id);
            if (urun != null)
            {
                db.Urunler.Remove(urun); 
                db.SaveChanges();        
            }
            return RedirectToAction("Urunler");
        }


        [Authorize]
        public ActionResult UrunGetir(int id)
        {
            var urun = db.Urunler.Find(id);
            if (urun == null) return HttpNotFound();

            return View(urun);
        }

        [Authorize]
        [HttpPost]
        public ActionResult UrunGuncelle(Urunler y)
        {
            if (ModelState.IsValid)
            {
                var urun = db.Urunler.Find(y.ID);
                if (urun == null) return HttpNotFound();

                urun.Barkod = y.Barkod;
                urun.UrunAd = y.UrunAd;
                urun.UrunFiyat = y.UrunFiyat;
                urun.UrunResmi = y.UrunResmi;
                urun.UrunKategori = y.UrunKategori;
                urun.KDVOran = y.KDVOran;
                urun.Stok = y.Stok;
                urun.GuncellenmeTarihi = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
                urun.Status = y.Status;
                urun.HizliUrunMu = y.HizliUrunMu;

                db.SaveChanges();

                TempData["GuncellemeBasarili"] = "Ürün başarıyla güncellendi!";
                return RedirectToAction("Urunler");
            }

            return View("UrunGetir", y);
        }

        [Authorize]
        [HttpGet]
        public ActionResult HizliUrunYap(int id)
        {
            var urun = db.Urunler.Find(id);
            if (urun == null)
                return HttpNotFound();

            // Toggle işlemi
            urun.HizliUrunMu = !urun.HizliUrunMu;
            urun.GuncellenmeTarihi = DateTime.Now.ToString("dd/MM/yyyy HH:mm");

            db.SaveChanges();
            return RedirectToAction("Urunler");
        }

        [Authorize]
        [HttpGet]
        public ActionResult EtiketOlustur(int[] seciliUrunler)
        {
            var urunler = db.Urunler.Where(m=>m.Status==true).ToList();
            return View(urunler);

        }




        [Authorize]
        [HttpPost]
        public ActionResult EtiketOlusturPDF(int[] seciliUrunler)
        {
            var urunler = (seciliUrunler == null || seciliUrunler.Length == 0)
                ? db.Urunler.ToList()
                : db.Urunler.Where(u => seciliUrunler.Contains(u.ID)).ToList();

            // YENİ AYARLAR (Yan yana 3 etiket)
            int labelsPerRow = 3; // Sabit 3 sütun
            float pageMargin = 15f; // Kenar boşlukları
            float spacingBetweenLabels = 10f; // Etiketler arası boşluk

            // Etiket genişliği = (A4 genişlik - kenar boşlukları - ara boşluklar) / 3
            float labelWidth = (PageSize.A4.Width - (pageMargin * 2) - (spacingBetweenLabels * (labelsPerRow - 1))) / labelsPerRow;
            float labelHeight = 3f * 28.35f; // 3cm yükseklik (sabit)

            using (MemoryStream memoryStream = new MemoryStream())
            {
                iTextSharp.text.Document document = new iTextSharp.text.Document(PageSize.A4, pageMargin, pageMargin, pageMargin, pageMargin);
                PdfWriter writer = PdfWriter.GetInstance(document, memoryStream);
                document.Open();

                // Türkçe font ayarı
                string fontPath = Environment.GetEnvironmentVariable("SystemRoot") + "\\fonts\\arial.ttf";
                BaseFont baseFont = BaseFont.CreateFont(fontPath, BaseFont.IDENTITY_H, BaseFont.EMBEDDED);

                // Fontlar
                Font productFont = new Font(baseFont, 11, Font.BOLD, BaseColor.BLACK); // Ürün adı biraz büyüdü
                Font priceFont = new Font(baseFont, 16, Font.BOLD, BaseColor.RED);   // Fiyat daha büyük
                Font kdvFont = new Font(baseFont, 7, Font.NORMAL, BaseColor.BLACK);    // KDV küçük
                Font lineFont = new Font(baseFont, 8, Font.NORMAL, BaseColor.LIGHT_GRAY);

                PdfPTable mainTable = new PdfPTable(labelsPerRow);
                mainTable.WidthPercentage = 100;
                mainTable.DefaultCell.Border = Rectangle.NO_BORDER;
                mainTable.DefaultCell.FixedHeight = labelHeight;
                mainTable.SpacingAfter = 15f; // Satırlar arası boşluk

                // Hücre padding ayarları
                float cellPadding = 8f;

                for (int i = 0; i < urunler.Count; i++)
                {
                    var urun = urunler[i];

                    string formattedPrice = decimal.TryParse(urun.UrunFiyat, out decimal priceValue)
                        ? priceValue.ToString("N2", new CultureInfo("tr-TR")) + "₺"
                        : "0,00₺";

                    PdfPCell cell = new PdfPCell();
                    cell.Border = Rectangle.NO_BORDER;
                    cell.Padding = cellPadding;
                    cell.PaddingBottom = cellPadding / 2; // Alt padding daha az

                    // Ürün adı (SOLA)
                    Paragraph urunAdParagraph = new Paragraph(urun.UrunAd.ToUpper(), productFont);
                    urunAdParagraph.Alignment = Element.ALIGN_LEFT;

                    // Fiyat (SAĞA)
                    Paragraph fiyatParagraph = new Paragraph(formattedPrice, priceFont);
                    fiyatParagraph.Alignment = Element.ALIGN_RIGHT;
                    fiyatParagraph.SpacingAfter = 4f;

                    // KDV (ORTA)
                    Paragraph kdvParagraph = new Paragraph("KDV Dahil Satış Fiyatıdır.", kdvFont);
                    kdvParagraph.Alignment = Element.ALIGN_CENTER;
                    kdvParagraph.SpacingAfter = 3f;

                    // Kesim çizgisi (tam genişlikte)
                    Paragraph cutLine = new Paragraph(new string('․', 30), lineFont); // Madde işareti kullandık
                    cutLine.Alignment = Element.ALIGN_CENTER;

                    cell.AddElement(urunAdParagraph);
                    cell.AddElement(fiyatParagraph);
                    cell.AddElement(kdvParagraph);
                    cell.AddElement(cutLine);

                    mainTable.AddCell(cell);

                    // Sayfa sonu kontrolü
                    if ((i + 1) % (labelsPerRow * 9) == 0) // Her 10 satırda yeni sayfa
                    {
                        document.Add(mainTable);
                        document.NewPage();
                        mainTable = new PdfPTable(labelsPerRow);
                        mainTable.WidthPercentage = 100;
                        mainTable.DefaultCell.Border = Rectangle.NO_BORDER;
                        mainTable.DefaultCell.FixedHeight = labelHeight;
                    }
                }

                // Eksik hücreleri tamamla
                int remainingCells = (labelsPerRow - (urunler.Count % labelsPerRow)) % labelsPerRow;
                for (int i = 0; i < remainingCells; i++)
                {
                    mainTable.AddCell(new PdfPCell(new Phrase(" ")) { Border = Rectangle.NO_BORDER });
                }

                document.Add(mainTable);
                document.Close();

                byte[] bytes = memoryStream.ToArray();
                return File(bytes, "application/pdf", "Etiketler.pdf");
            }
        }


        public JsonResult BarkodAra(string barkod)
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







    }
}
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

            int labelsPerRow = 3;
            float pageMargin = 15f;
            float spacingBetweenLabels = 10f;

            float labelWidth = (PageSize.A4.Width - (pageMargin * 2) - (spacingBetweenLabels * (labelsPerRow - 1))) / labelsPerRow;
            float labelHeight = 3f * 28.35f;

            using (MemoryStream memoryStream = new MemoryStream())
            {
                iTextSharp.text.Document document = new iTextSharp.text.Document(PageSize.A4, pageMargin, pageMargin, pageMargin, pageMargin);
                PdfWriter writer = PdfWriter.GetInstance(document, memoryStream);
                document.Open();

                string fontPath = Environment.GetEnvironmentVariable("SystemRoot") + "\\fonts\\arial.ttf";
                BaseFont baseFont = BaseFont.CreateFont(fontPath, BaseFont.IDENTITY_H, BaseFont.EMBEDDED);

                Font productFont = new Font(baseFont, 11, Font.BOLD, BaseColor.BLACK);
                Font priceFont = new Font(baseFont, 16, Font.BOLD, BaseColor.RED);
                Font kdvFont = new Font(baseFont, 7, Font.NORMAL, BaseColor.BLACK);
                Font lineFont = new Font(baseFont, 8, Font.NORMAL, BaseColor.LIGHT_GRAY);

                PdfPTable mainTable = new PdfPTable(labelsPerRow);
                mainTable.WidthPercentage = 100;
                mainTable.DefaultCell.Border = Rectangle.NO_BORDER;
                mainTable.DefaultCell.FixedHeight = labelHeight;
                mainTable.SpacingAfter = 15f;

                float cellPadding = 8f;

                // Burada fiyat parse işlemini düzeltelim:
                var invariantCulture = System.Globalization.CultureInfo.InvariantCulture;
                var turkishCulture = new System.Globalization.CultureInfo("tr-TR");

                for (int i = 0; i < urunler.Count; i++)
                {
                    var urun = urunler[i];

                    decimal priceValue;
                    // Önce InvariantCulture ile dene, başarısızsa Türkçe ile dene, yoksa 0 yap
                    if (!decimal.TryParse(urun.UrunFiyat, System.Globalization.NumberStyles.Any, invariantCulture, out priceValue) &&
                        !decimal.TryParse(urun.UrunFiyat, System.Globalization.NumberStyles.Any, turkishCulture, out priceValue))
                    {
                        priceValue = 0m;
                    }

                    string formattedPrice = priceValue.ToString("N2", turkishCulture) + " ₺";

                    PdfPCell cell = new PdfPCell();
                    cell.Border = Rectangle.NO_BORDER;
                    cell.Padding = cellPadding;
                    cell.PaddingBottom = cellPadding / 2;

                    Paragraph urunAdParagraph = new Paragraph(urun.UrunAd.ToUpper(), productFont);
                    urunAdParagraph.Alignment = Element.ALIGN_LEFT;

                    Paragraph fiyatParagraph = new Paragraph(formattedPrice, priceFont);
                    fiyatParagraph.Alignment = Element.ALIGN_RIGHT;
                    fiyatParagraph.SpacingAfter = 4f;

                    Paragraph kdvParagraph = new Paragraph("KDV Dahil Satış Fiyatıdır.", kdvFont);
                    kdvParagraph.Alignment = Element.ALIGN_CENTER;
                    kdvParagraph.SpacingAfter = 3f;

                    Paragraph cutLine = new Paragraph(new string('․', 30), lineFont);
                    cutLine.Alignment = Element.ALIGN_CENTER;

                    cell.AddElement(urunAdParagraph);
                    cell.AddElement(fiyatParagraph);
                    cell.AddElement(kdvParagraph);
                    cell.AddElement(cutLine);

                    mainTable.AddCell(cell);

                    if ((i + 1) % (labelsPerRow * 9) == 0)
                    {
                        document.Add(mainTable);
                        document.NewPage();
                        mainTable = new PdfPTable(labelsPerRow);
                        mainTable.WidthPercentage = 100;
                        mainTable.DefaultCell.Border = Rectangle.NO_BORDER;
                        mainTable.DefaultCell.FixedHeight = labelHeight;
                    }
                }

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
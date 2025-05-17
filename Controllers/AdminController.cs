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
            // Toplam satış tutarı
            decimal toplamSatisTutar = db.SatisIslem.Sum(s => (decimal?)s.ToplamTutar) ?? 0;

            // Ödeme tiplerine göre satış adedi ve toplam tutar
            var odemeTipiIstatistik = db.SatisIslem
                .GroupBy(s => s.OdemeTipi)
                .Select(g => new
                {
                    OdemeTipi = g.Key,
                    Adet = g.Count(),
                    ToplamTutar = g.Sum(x => (decimal?)x.ToplamTutar) ?? 0
                }).ToList();

            // ViewBag ile verileri gönderiyoruz
            ViewBag.ToplamSatisTutar = toplamSatisTutar;
            ViewBag.OdemeTipiIstatistik = odemeTipiIstatistik;

            return View();
        }

    }
}
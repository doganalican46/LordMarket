using LordMarket.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace LordMarket.Controllers
{
    public class SatisYapController : Controller
    {

        public class SatisIslemViewModel
        {
            public List<GelirGider> GelirGider { get; set; }
            public List<Kategoriler> Kategoriler { get; set; }
            public List<Musteriler> Musteriler { get; set; }
            public List<Satislar> Satislar { get; set; }
            public List<SatisIslem> SatisIslem { get; set; }
            public List<Urunler> Urunler { get; set; }
            public List<UserController> UserController { get; set; }

        }


        // GET: SatisYap
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Inde2x()
        {
            return View();
        }
        public ActionResult Index3()
        {
            return View();
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using AuditLog.Models;
using System.Web.Mvc;
using Newtonsoft.Json;

namespace AuditLog.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index(bool ShowDeleted = false)
        { 
            SampleDataModel SD = new SampleDataModel();
            return View(SD.GetAllData(ShowDeleted));
        }


        public ActionResult Edit(int id)
        {
            SampleDataModel SD = new SampleDataModel();
            return View(SD.GetData(id));
        }

        public ActionResult Create()
        {
            SampleDataModel SD = new SampleDataModel();
            SD.ID = -1; // indicates record not yet saved
            SD.DateOfBirth = DateTime.Now.AddYears(-25);
            return View("Edit", SD);
        }

        public void Delete(int id)
        {
            SampleDataModel SD = new SampleDataModel();
            SD.DeleteRecord(id);
        }

        public ActionResult Save(SampleDataModel Rec)
        {
            SampleDataModel SD = new SampleDataModel();
            if (Rec.ID == -1)
            {
                SD.CreateRecord(Rec);
            }
            else
            {
                SD.UpdateRecord(Rec);
            }
            return Redirect("/");
        }

        public JsonResult Audit(int id)
        {
            SampleDataModel SD = new SampleDataModel();
            var AuditTrail = SD.GetAudit(id);
            return Json(AuditTrail, JsonRequestBehavior.AllowGet);
        }


    }
}
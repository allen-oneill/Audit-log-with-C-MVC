using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using KellermanSoftware.CompareNetObjects;
using System.Xml.Serialization;
using System.IO;
using Newtonsoft.Json;

/// <summary>
///  NB !!! ... dont forget to carry out the following steps to run this example
/// 
///  (1) Create your database (mine is called "AuditTest") and then RUN the table create script against that database
///  (2) Change the Web.Config to point to your database
/// </summary>

namespace AuditLog.Models
{
    public class SampleDataModel
    {
        public int ID { get; set; }
        public string FirstName { get; set; }
        public string lastname { get; set; }
        public DateTime DateOfBirth { get; set; }
        public bool Deleted { get; set; }


        public SampleDataModel GetData(int ID)
        {
            SampleDataModel mod = new SampleDataModel();
            AuditTestEntities ent = new AuditTestEntities();
            SampleData rec = ent.SampleData.FirstOrDefault(s => s.ID == ID);
            if (rec != null) {
                mod.ID = rec.ID;
                mod.FirstName = rec.FirstName;
                mod.lastname = rec.LastName;
                mod.DateOfBirth = rec.DateOfBirth;
            }
            return mod;
        }

        public void DeleteRecord(int ID)
        {
            AuditTestEntities ent = new AuditTestEntities();
            SampleData rec = ent.SampleData.FirstOrDefault(s => s.ID == ID);
            if (rec != null)
            {
                SampleData DummyObject = new SampleData(); // Storage of this null object shows data after delete = nix, naught, nothing!
                rec.Deleted = true;
                ent.SaveChanges();
                CreateAuditTrail(AuditActionType.Delete, ID, rec, DummyObject);
            }
        }

        public void CreateAuditTrail (AuditActionType Action, int KeyFieldID, Object OldObject, Object NewObject)
        {
            // get the differance
            CompareLogic compObjects = new CompareLogic();
            compObjects.Config.MaxDifferences = 99;
            ComparisonResult compResult = compObjects.Compare(OldObject, NewObject);
            List<AuditDelta> DeltaList = new List<AuditDelta>();
            foreach (var change in compResult.Differences)
            {
                AuditDelta delta = new AuditDelta();
                if (change.PropertyName.Substring(0, 1) == ".")
                    delta.FieldName = change.PropertyName.Substring(1, change.PropertyName.Length - 1);
                delta.ValueBefore = change.Object1Value;
                delta.ValueAfter = change.Object2Value;
                DeltaList.Add(delta);
            }
            AuditTable audit = new AuditTable();
            audit.AuditActionTypeENUM = (int)Action;
            audit.DataModel = this.GetType().Name;
            audit.DateTimeStamp = DateTime.Now;
            audit.KeyFieldID = KeyFieldID;
            audit.ValueBefore = JsonConvert.SerializeObject(OldObject); // if use xml instead of json, can use xml annotation to describe field names etc better
            audit.ValueAfter = JsonConvert.SerializeObject(NewObject);
            audit.Changes = JsonConvert.SerializeObject(DeltaList);
            AuditTestEntities ent = new AuditTestEntities();
            ent.AuditTable.Add(audit);
            ent.SaveChanges();

        }

        public List<SampleDataModel> GetAllData(bool ShowDeleted)
        {
            List<SampleDataModel> rslt = new List<SampleDataModel>();
            AuditTestEntities ent = new AuditTestEntities();
            List<SampleData> SearchResults = new List<SampleData>();

            if (ShowDeleted)
                SearchResults = ent.SampleData.ToList();
            else SearchResults = ent.SampleData.Where(s => s.Deleted == false).ToList();

            foreach (var record in SearchResults)
            {
                SampleDataModel rec = new SampleDataModel();
                rec.ID = record.ID;
                rec.FirstName = record.FirstName;
                rec.lastname = record.LastName;
                rec.DateOfBirth = record.DateOfBirth;
                rec.Deleted = record.Deleted;
                rslt.Add(rec);
            }
            return rslt;
        }

        public bool UpdateRecord(SampleDataModel Rec)
        {
            bool rslt = false;
            AuditTestEntities ent = new AuditTestEntities();
            var dbRec = ent.SampleData.FirstOrDefault(s => s.ID == Rec.ID);
            if (dbRec != null) {
                // audit process 1 - gather old values
                SampleDataModel OldRecord = new SampleDataModel();
                OldRecord.ID = dbRec.ID;
                OldRecord.FirstName = dbRec.FirstName;
                OldRecord.lastname = dbRec.LastName;
                OldRecord.DateOfBirth = dbRec.DateOfBirth;
                // update the live record
                dbRec.FirstName = Rec.FirstName;
                dbRec.LastName = Rec.lastname;
                dbRec.DateOfBirth = Rec.DateOfBirth;
                ent.SaveChanges();

                CreateAuditTrail(AuditActionType.Update, Rec.ID, OldRecord, Rec);

                rslt = true;
            }
            return rslt;
        }

        public void CreateRecord(SampleDataModel Rec)
        {

            AuditTestEntities ent = new AuditTestEntities();
            SampleData dbRec = new SampleData();
            dbRec.FirstName = Rec.FirstName;
            dbRec.LastName = Rec.lastname;
            dbRec.DateOfBirth = Rec.DateOfBirth;
            ent.SampleData.Add(dbRec);
            ent.SaveChanges(); // save first so we get back the dbRec.ID for audit tracking
            SampleData DummyObject = new SampleData(); // Storage of this null object shows data before creation = nix, naught, nothing!

            CreateAuditTrail(AuditActionType.Create, dbRec.ID, DummyObject, dbRec);

        }


        public List<AuditChange> GetAudit(int ID)
        {
            List<AuditChange> rslt = new List<AuditChange>();
            AuditTestEntities ent = new AuditTestEntities();
            var AuditTrail = ent.AuditTable.Where(s => s.KeyFieldID == ID).OrderByDescending(s => s.DateTimeStamp); // we are looking for audit-history of the record selected.
            var serializer = new XmlSerializer(typeof(AuditDelta));
            foreach (var record in AuditTrail)
            {
                AuditChange Change = new AuditChange();
                Change.DateTimeStamp = record.DateTimeStamp.ToString();
                Change.AuditActionType = (AuditActionType)record.AuditActionTypeENUM;
                Change.AuditActionTypeName = Enum.GetName(typeof(AuditActionType),record.AuditActionTypeENUM);
                List<AuditDelta> delta = new List<AuditDelta>();
                delta = JsonConvert.DeserializeObject<List<AuditDelta>>(record.Changes);
                Change.Changes.AddRange(delta);
                rslt.Add(Change);
            }
            return rslt;
        }


    }

    public class AuditChange {
       public string DateTimeStamp { get; set; }
        public AuditActionType AuditActionType { get; set; }
        public string AuditActionTypeName { get; set; }
        public List<AuditDelta> Changes { get; set; }
        public AuditChange()
        {
            Changes = new List<AuditDelta>();
        }
    }

    public class AuditDelta {
        public string FieldName { get; set; }
        public string ValueBefore { get; set; }
        public string ValueAfter { get; set; }
    }

    public enum AuditActionType {
        Create = 1,
        Update,
        Delete
    }


}
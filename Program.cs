using System;
using System.IO;
using System.Collections.Generic;
// using System.Security.Permissions;
using Newtonsoft.Json;

namespace UnityReportUnpacker
{

    public class Attachment
    {
        public string contentType;
        public string dataBase64;
        public string dataIdentifier;
        public string fileName;
        public string frameNumber;
        public int height;
        public int width;
        public string name;
    }

    public class Report
    {
        public List<Attachment> attachments;
        public List<Attachment> screenshots;
        public string identifier;
        public string summary;
        public Attachment thumbnail;
    }

    public class GenericObject : Dictionary<string, object> {};

    class Program
    {
        static void Main(string[] args)
        {
            var dirname = Directory.GetCurrentDirectory();
            List<string> files = new List<string>(Directory.EnumerateFiles(dirname)).FindAll(x => x.EndsWith("json"));

            string jsonString;
            Console.WriteLine("FILES "+files.Count);
            Report report;
            string reportDirname;
            string reportDirPath;
            string summaryShort;
            foreach (var file in files)
            {
                jsonString = File.ReadAllText(file);                
                report = JsonConvert.DeserializeObject<Report>(jsonString);
                summaryShort = report.summary.Replace(" ","_");
                summaryShort = summaryShort.Replace("/","");
                summaryShort = summaryShort.Replace("\\","");
                if (summaryShort.Length > 20)
                {
                    summaryShort = summaryShort.Substring(0,20);
                }
                reportDirname = $"{report.identifier}_{summaryShort}";    
                reportDirPath = Path.Combine(dirname, reportDirname);
                if (Directory.Exists(reportDirPath))
                {
                    Directory.Delete(reportDirPath, true);
                }
                Directory.CreateDirectory(reportDirPath);

                // Export attachments.
                foreach (var attachment in report.attachments)
                {
                    SaveFile(attachment, reportDirPath, attachment.fileName);
                }

                // Export screenshots.
                foreach (var attachment in report.screenshots)
                {
                    SaveFile(attachment, reportDirPath, attachment.height+"_"+attachment.dataIdentifier+".png");                    
                }
                SaveFile(report.thumbnail, reportDirPath, "thumbnail.png");
                Console.WriteLine("FILE "+Path.GetFileNameWithoutExtension(file));

                // Export sparse report.
                GenericObject reportObject = JsonConvert.DeserializeObject<GenericObject>(jsonString);
                reportObject["screenshots"] = null;
                reportObject["attachments"] = null;
                reportObject["thumbnail"] = null;

                // Export into separate files.
                var separateExports = new string[] { 
                    "clientMetrics", "deviceMetadata",
                    "events", "measures", "aggregateMetrics",
                };

                foreach (var export in separateExports)
                {
                    File.WriteAllText(
                        Path.Combine(reportDirPath, export+".json"),
                        JsonConvert.SerializeObject(reportObject[export], Formatting.Indented)
                    );
                    reportObject[export] = null;
                }

                File.WriteAllText(
                    Path.Combine(reportDirPath, "report.json"),
                    JsonConvert.SerializeObject(reportObject, Formatting.Indented)
                );
            }
        }

        static void SaveFile(Attachment attachment, string dir, string name)
        {
            var bytes = System.Convert.FromBase64String(attachment.dataBase64);
            File.WriteAllBytes(
                Path.Combine(dir, name),
                bytes
            );
        }
    }
}

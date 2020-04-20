using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using FeatureExporter.Mongo;
using Newtonsoft.Json;

namespace FeatureExporter
{
    class Program
    {
        static void Main(string[] args)
        {
            Log.Initialize("log.txt", LogLevel.All, false);

            var nsettings = new Settings();
            
            if (!File.Exists("settings.json"))
            {
                nsettings = new Settings()
                {
                    Key = "glGueV2hkNEZgQ5awAZV6lN1iAUK3p5l",    // and this
                    Keywords = new List<string>()
                    {
                        "coronavirus",
                        "covid-19",
                        "covid"
                    },
                    StartPage = 0,
                    TotalPages = 5,
                    Cookies = "/home/elio/Downloads/cookies.json",  // update this accordingly
                    CheckExisting = true,
                    User = "Elio"     // also this
                };
                
                // update settings
                var json = JsonConvert.SerializeObject(nsettings);
                File.WriteAllText("settings.json", json);
            }
            else
            {
                var json = File.ReadAllText("settings.json");
                nsettings = JsonConvert.DeserializeObject<Settings>(json);
            }
            
            var exp = new NYTimesExport(nsettings);
            exp.Export();

            var mongo = new MongoUtils(nsettings);
            mongo.SaveArticles(exp.Articles);
            mongo.StartUploadingContents();
        }
    }
}
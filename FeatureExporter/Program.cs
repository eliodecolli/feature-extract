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
        static List<Keyword> DefaultKeywords()
        {
            var kw1 = new Keyword()
            {
                Value = "cases",
                Boosters = new List<string>()
                {
                    "rise", "risen", "more", "highest", "high"
                }
            };
            
            var kw2 = new Keyword()
            {
                Value = "deaths",
                Boosters = new List<string>()
                {
                    "rise", "risen", "more", "highest", "high"
                }
            };
            
            var kw3 = new Keyword()
            {
                Value = "gatherings",
                Boosters = new List<string>()
                {
                    "more", "seen", "been"
                }
            };
            
            var kw4 = new Keyword()
            {
                Value = "died",
                Boosters = new List<string>()
                {
                    "has", "have", "more"
                }
            };
            
            var kw5 = new Keyword()
            {
                Value = "tested",
                Boosters = new List<string>()
                {
                    "positive", "more", "being"
                }
            };
            
            var kw6 = new Keyword()
            {
                Value = "tragedy",
                Boosters = new List<string>()
            };

            return new List<Keyword>()
            {
                kw1, kw2, kw3, kw4, kw5
            };
        }
        
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
                    StartPage = 34,
                    TotalPages = 9,
                    Cookies = "/home/elio/Downloads/cookies.json",  // update this accordingly
                    CheckExisting = true,
                    User = "Elio",     // also this
                    ConcernKeywords = DefaultKeywords()
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
            
            //mongo.UpdateConcerns();

            /*var tt = File.ReadAllBytes("test.txt");
            var cc = new ArticleConcernAnalyzer(tt, nsettings.ConcernKeywords.ToArray());

            Log.Info(cc.RetrieveConcern().ToString());*/
        }
    }
}
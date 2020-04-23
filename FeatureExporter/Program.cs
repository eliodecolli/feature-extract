/*
 *    File Name:
 *         Program.cs
 * 
 *    Purpose:
 *         Experimental stuff.
 *
 *     Author:
 *         Elio Decolli
 * 
 *     Last Updated:
 *         22/04/2020 - 10:34 PM
 *
 *     TODO:
 *         [] Perhaps turn this into a CLI ?
 */

using System.Collections.Generic;
using System.IO;
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
                    Key = "api-key",    // and this
                    Keywords = new List<string>()
                    {
                        "coronavirus",
                        "covid-19",
                        "covid"
                    },
                    StartPage = 34,
                    TotalPages = 9,
                    Cookies = "/Path/To/cookies.json",  // update this accordingly
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
            
            // some stuff..
            
            //var exp = new NYTimesExport(nsettings);
            //exp.Export();

            //var mongo = new MongoUtils(nsettings);
            //mongo.SaveOnDisk();
            //mongo.SaveArticles(exp.Articles);
            //mongo.StartUploadingContents();*/
            
            //mongo.UpdateConcerns();
            //mongo.UpdateConcernsLocally();
            
            //mongo.GenerateCSVLocally();
        }
    }
}
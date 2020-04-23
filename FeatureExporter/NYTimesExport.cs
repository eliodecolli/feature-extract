/*
 *    File Name:
 *         NYTimesExport.cs
 * 
 *    Purpose:
 *         Use the New York Times API to retrieve articles containing a given set of keywords.
 *
 *     Author:
 *         Elio Decolli
 * 
 *     Last Updated:
 *         22/04/2020 - 10:34 PM
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using Newtonsoft.Json.Linq;

namespace FeatureExporter
{
    /// <summary>
    /// An article retrieved from the API.
    /// </summary>
    public class Article
    {
        public string ID { get; private set; }
        
        public string Title { get; private set; }
        
        public DateTime OriginalDate { get; private set; }
        
        public string Url { get; private set; }
        
        public string User { get; private set; }

        public Article(JToken part, string user)
        {
            User = user;

            ID = part["_id"].Value<string>();
            
            Title = part["headline"]["main"].Value<string>();
            
            Url = part["web_url"].Value<string>();
            string dt = part["pub_date"].Value<string>();
            OriginalDate = DateTime.Parse(dt);
        }
    }
    
    public class NyTimesExport
    {
        public List<Article> Articles { get; private set; }

        private Settings settings;
        
        
         public NyTimesExport(Settings settings)
         {
             Log.Info("Extracting cookies");
             this.settings = settings;
         }

         /// <summary>
         /// Updates the Articles property, either from the cached file(s) on disk or from a new API call.
         /// </summary>
         public void Export()
         {
             bool flag = false;
             foreach (var path in Directory.GetFiles(Environment.CurrentDirectory))
             {
                 if (Path.GetFileNameWithoutExtension(path).StartsWith("articles_ny_") && settings.CheckExisting)
                 {
                     flag = true;
                     break;
                 }
             }
             Articles = new List<Article>();

             Log.Info($"Getting articles with keywords {string.Join(",", settings.Keywords)}");

             if (!flag)
             {
                 var total = settings.StartPage + settings.TotalPages;
             
                 for (int i = settings.StartPage; i <= total; i++)
                 {
                     string url = $"https://api.nytimes.com/svc/search/v2/articlesearch.json?q={string.Join(",", settings.Keywords)}&api-key={settings.Key}&page={i}";
                     var req = (HttpWebRequest) WebRequest.CreateHttp(url);
                     req.Method = "GET";

                     var response = req.GetResponse();
             
                     var stream = response.GetResponseStream();
                     using var f = File.Open($"articles_ny_{i}.json", FileMode.OpenOrCreate);
                     stream.CopyTo(f);
                     f.Flush();

                     Log.Info($"Got page {i}/{total}");
                 }
             }
             else
             {
                 Log.Info("Articles seem to exist already. Using disk data...");
             }

             foreach (var filePath in Directory.GetFiles(Environment.CurrentDirectory))
             {
                 if(!Path.GetFileNameWithoutExtension(filePath).StartsWith("articles_ny_"))
                     continue;
                 
                 var json = JObject.Parse(File.ReadAllText(filePath));
                 var docs = json["response"]["docs"];
             
                 foreach (var token in docs)
                 {
                     Articles.Add(new Article(token, settings.User));
                 }
             }
         }
    }

    
}
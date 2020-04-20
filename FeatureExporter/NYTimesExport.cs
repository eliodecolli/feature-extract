using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace FeatureExporter
{
    public class Article
    {
        public string ID { get; private set; }
        
        public string Title { get; private set; }
        
        public string Content { get; private set; }
        
        public DateTime OriginalDate { get; private set; }
        
        public string Url { get; private set; }
        
        public string User { get; private set; }

        private CookieCollection cookies;

        public Article(JToken part, CookieCollection cookies, string user)
        {
            User = user;
            this.cookies = cookies;

            ID = part["_id"].Value<string>();
            
            Title = part["headline"]["main"].Value<string>();
            
            Url = part["web_url"].Value<string>();
            string dt = part["pub_date"].Value<string>();
            OriginalDate = DateTime.Parse(dt);
        }

        public void UpdateContent()
        {
            
        }
    }
    
    public class NYTimesExport
    {
        public List<Article> Articles { get; private set; }

        private Settings settings;
        
        
         public NYTimesExport(Settings settings)
         {
             Log.Info("Extracting cookies");
             this.settings = settings;
         }

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
             
             var jar = new CookieCollection();
             
             var job = JArray.Parse(File.ReadAllText(settings.Cookies));

             foreach (var token in job)
             {
                 jar.Add(new Cookie(token["Name raw"].ToString(), token["Content raw"].ToString(), token["Path raw"].ToString(), "nytimes.com"));
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
                     Articles.Add(new Article(token, jar, settings.User));
                 }
             }
         }
    }

    
}
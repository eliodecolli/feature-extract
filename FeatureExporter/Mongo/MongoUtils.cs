using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Security.Authentication;
using MongoDB.Driver;
using Newtonsoft.Json.Linq;

namespace FeatureExporter.Mongo
{
    public class MongoUtils
    {
        private IMongoClient client;
        private Settings settings;

        public MongoUtils(Settings settings)
        {
            this.settings = settings;
            
            string connectionString = 
                @"mongodb://a-mongo:CsCblbWqYyULCtVlNXScqFgusbDbiramYAL8jqRtqFqeg6CjLNv8vBDWRxNy0oPmt76QAaXWevq4kkVICA6log==@a-mongo.mongo.cosmos.azure.com:10255/?ssl=true&replicaSet=globaldb&retrywrites=false&maxIdleTimeMS=120000&appName=@a-mongo@";
            MongoClientSettings mongoSettings = MongoClientSettings.FromUrl(
                new MongoUrl(connectionString)
            );
            mongoSettings.SslSettings = 
                new SslSettings() { EnabledSslProtocols = SslProtocols.Tls12 };
            client = new MongoClient(mongoSettings);
        }

        public void SaveArticles(List<Article> articles)
        {
            Log.Info("Saving articles on Mongo..");

            var collection = client.GetDatabase("ny_times").GetCollection<ArchivedMongoArticle>("articles");

            articles.ForEach(x =>
            {
                var ach = new ArchivedMongoArticle()
                {
                    DatePublished = x.OriginalDate,
                    Done = false,
                    ConcernLevel = 0,
                    ArticleId = x.ID,
                    GZipContents = new byte[0],
                    Title = x.Title,
                    Url = x.Url,
                    User = x.User
                };

                var found = collection.Find(Builders<ArchivedMongoArticle>.Filter.Eq(x => x.ArticleId, ach.ArticleId));

                if (found.CountDocuments() > 0)
                {
                    Log.Info($"Article {ach.ArticleId} already exists in our records.");
                }
                else
                {
                    collection.InsertOne(ach);
                    Log.Info($"Inserted article with id {ach.ArticleId}");
                }
            });
            
            Log.Info($"Inserted {articles.Count} articles");
        }

        public void StartUploadingContents()
        {
            Log.Info($"[{settings.User}] Getting contents for articles...");
            
            var collection = client.GetDatabase("ny_times").GetCollection<ArchivedMongoArticle>("articles");

            var userFilter = Builders<ArchivedMongoArticle>.Filter.Eq(x => x.User, settings.User);
            var doneFilter = Builders<ArchivedMongoArticle>.Filter.Eq(x => x.Done, false);
            var contentsFilter = Builders<ArchivedMongoArticle>.Filter.Eq(x => x.GZipContents, new byte[0]);

            var articles = collection.Find(Builders<ArchivedMongoArticle>.Filter.And(userFilter, doneFilter, contentsFilter));

            if (articles.CountDocuments() == 0)
            {
                Log.Error("No articles were found. Maybe they're all processed? Try using MR in order to aggregate the results.");
                return;
            }
            
            var jar = new CookieCollection();
             
            var job = JArray.Parse(File.ReadAllText(settings.Cookies));

            foreach (var token in job)
            {
                jar.Add(new Cookie(token["Name raw"].ToString(), token["Content raw"].ToString(), token["Path raw"].ToString(), "nytimes.com"));
            }

            foreach (var article in articles.ToList())
            {
                Log.Info($"Trying to update content for article \"{article.ArticleId}\"");

                var req = (HttpWebRequest) WebRequest.CreateHttp(article.Url);
                req.Method = "GET";
            
                req.CookieContainer = new CookieContainer();
                req.CookieContainer.Add(jar);

                var res = req.GetResponse();
            
                using var temp = new MemoryStream();
                using var f = new GZipStream(temp, CompressionMode.Compress);
                
                res.GetResponseStream().CopyTo(f);
                f.Flush();  // now it's on temp :)
                
                Log.Info($"Total bytes: {temp.Length}");

                article.GZipContents = temp.ToArray();

                collection.FindOneAndUpdate(Builders<ArchivedMongoArticle>.Filter.Eq(x => x._id, article._id),
                    new ObjectUpdateDefinition<ArchivedMongoArticle>(article));
            }
            
            Log.Info("All articles have been updated :)");
        }

        public void UpdateConcerns()
        {
            Log.Info($"[{settings.User}] Updating concern levels...");
            
            var collection = client.GetDatabase("ny_times").GetCollection<ArchivedMongoArticle>("articles");

            var userFilter = Builders<ArchivedMongoArticle>.Filter.Eq(x => x.User, settings.User);
            //var doneFilter = Builders<ArchivedMongoArticle>.Filter.Eq(x => x.Done, false);
            var contentsFilterBase = Builders<ArchivedMongoArticle>.Filter.Eq(x => x.GZipContents, new byte[0]);
            var contentsFilter = Builders<ArchivedMongoArticle>.Filter.Not(contentsFilterBase);
            
            var articles = collection.Find(Builders<ArchivedMongoArticle>.Filter.And(userFilter/*, doneFilter*/, contentsFilter));

            if (articles.CountDocuments() > 0)
            {
                foreach (var article in articles.ToList())
                {
                    File.WriteAllBytes("test.txt", article.GZipContents);
                    return;
                    Log.Info($"Updating concern level for article \"{article.ArticleId}\"...");
                    var concernAnalyzer = new ArticleConcernAnalyzer(article.GZipContents, settings.ConcernKeywords.ToArray());

                    article.ConcernLevel = concernAnalyzer.RetrieveConcern();
                    article.Done = true;
                    collection.FindOneAndUpdate(Builders<ArchivedMongoArticle>.Filter.Eq(x => x._id, article._id),
                        new ObjectUpdateDefinition<ArchivedMongoArticle>((article)));
                }
            }
            else
            {
                Log.Info("All concern levels seem to be updated!");
            }
        }
    }
}
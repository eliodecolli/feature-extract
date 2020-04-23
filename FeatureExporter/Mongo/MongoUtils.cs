/*
 *    File Name:
 *         MongoUtils.cs
 * 
 *    Purpose:
 *         Contains methods and operations in order to manipulate the dataset. Locally and remotely (on Cosmos DB).
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
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Security.Authentication;
using System.Text;
using MongoDB.Driver;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace FeatureExporter.Mongo
{
    
    public class MongoUtils
    {
        private IMongoClient client;
        private Settings settings;

        public MongoUtils(Settings settings)
        {
            this.settings = settings;

            string connectionString = "your-connection-string";
            
             MongoClientSettings mongoSettings = MongoClientSettings.FromUrl(
                new MongoUrl(connectionString)
            );
            mongoSettings.SslSettings = 
                new SslSettings() { EnabledSslProtocols = SslProtocols.Tls12 };
            client = new MongoClient(mongoSettings);
        }

        /// <summary>
        /// Saves the exported articles information from the API on Cosmos.
        /// </summary>
        /// <param name="articles">The exported articles.</param>
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
                    collection.InsertOne(ach);     // maybe use BulkWrite() ?
                    Log.Info($"Inserted article with id {ach.ArticleId}");
                }
            });
            
            Log.Info($"Inserted {articles.Count} articles");
        }

        /// <summary>
        /// Uses the given cookies to submit a Http Request to the specified article URL in order to retrieve the HTML contents.
        /// It then compresses them as GZip and uploads them.
        /// </summary>
        /// <remarks>This uploads data inside the Cosmos DB, using a Blob storage would be a better approach.</remarks>
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

        /// <summary>
        /// Downloads all the updated articles on Mongo, and saves them locally.
        /// </summary>
        public void SaveOnDisk()
        {
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
                    Log.Info($"Saving article \"{article.ArticleId}\"...");

                    using var memsr = File.Create(Path.Combine(Environment.CurrentDirectory, "Data",
                        Convert.ToBase64String(article._id.ToByteArray()).Replace("/", "_") + ".dat"));
                    using var writer = new BinaryWriter(memsr);
                    
                    /*
                     * Format:
                     * (String) - Article Id
                     * (Int64) - Publication Date
                     * (Int32) - Contents Length
                     * (Byte[]) - Contents
                     */
                    
                    writer.Write(article.ArticleId);
                    writer.Write(article.DatePublished.ToBinary());
                    writer.Write(article.GZipContents.Length);
                    writer.Write(article.GZipContents);
                    writer.Flush();
                    
                    memsr.Flush();
                }
            }
            else
            {
                Log.Info("All concern levels seem to be updated!");
            }
        }
        
        /// <summary>
        /// Analyzes the saved article locally and retrieves a concern level.
        /// </summary>
        /// <param name="file">The article on disk.</param>
        private void Update(string file)
        {

                using var fs = File.Open(file, FileMode.Open);
                using var reader = new BinaryReader(fs);

                var title = reader.ReadString();
                var date = DateTime.FromBinary(reader.ReadInt64());
                var len = reader.ReadInt32();

                var blob = reader.ReadBytes(len);
                var analyzer = new ArticleConcernAnalyzer(blob, settings.ConcernKeywords.ToArray());

                var concern = analyzer.RetrieveConcern();

                var ar = new ArchivedMongoArticle()
                {
                    ConcernLevel = concern,
                    DatePublished = date,
                    ArticleId = title
                };
                
                Log.Info($"Updated \"{title}\"");
            
                var json = JsonConvert.SerializeObject(ar);
                File.WriteAllText(Path.Combine(Environment.CurrentDirectory, "Data", "Updated", Path.GetFileNameWithoutExtension(file) + ".json"), json);
        }

        /// <summary>
        /// Updates the concern level for every article saved locally.
        /// </summary>
        public void UpdateConcernsLocally()
        {
            var cc = Directory.GetFiles("Data").Where(x => x.EndsWith(".dat")).ToArray();
            
            foreach (var i in cc)
            {
                Update(i);
            }
        }

        /// <summary>
        /// Reads the concern level of the articles locally and generates a CSV representing the data.
        /// </summary>
        public void GenerateCSVLocally()
        {
            var tt = new List<ArchivedMongoArticle>();
            
            foreach (var file in Directory.GetFiles(Path.Combine(Environment.CurrentDirectory, "Data", "Updated")))
            {
                if (file.EndsWith(".json"))
                {
                    var obj = JsonConvert.DeserializeObject<ArchivedMongoArticle>(File.ReadAllText(file));
                    tt.Add(obj);
                }
            }
            
            var dict = new SortedDictionary<DateTime, int>();
            tt.ForEach(x =>
            {
                var date = x.DatePublished.Date;

                if (!dict.ContainsKey(date))
                    dict.Add(date, 0);
                
                dict[date] += x.ConcernLevel;
            });
            Log.Info($"Got a total of {dict.Keys.Count} dates.");
            
            
            /*
             * Format:
             * ----------------------------------
             * | Date        |    Concern Level |
             * ----------------------------------
             * | dd/MM/yyy   |    Int32         |
             * ----------------------------------
             */
            
            var csv = new StringBuilder();
            csv.AppendLine("Date,Concern Level");

            foreach (var damn in dict)
            {
                csv.AppendLine($"{damn.Key.ToString("dd/MM/yyy")},{damn.Value}");
            }
            
            File.WriteAllText("concern_levels.csv", csv.ToString());
        }

        /// <summary>
        /// Updates the concern level of every article and stores the result on another collection, without the article contents.
        /// </summary>
        public void UpdateConcerns()
        {
            Log.Info($"[{settings.User}] Updating concern levels...");
            
            var collection = client.GetDatabase("ny_times").GetCollection<ArchivedMongoArticle>("articles");
            var updated_col = client.GetDatabase("ny_times").GetCollection<ArchivedMongoArticle>("articles_done");

            var userFilter = Builders<ArchivedMongoArticle>.Filter.Eq(x => x.User, settings.User);
            //var doneFilter = Builders<ArchivedMongoArticle>.Filter.Eq(x => x.Done, false);
            var contentsFilterBase = Builders<ArchivedMongoArticle>.Filter.Eq(x => x.GZipContents, new byte[0]);
            var contentsFilter = Builders<ArchivedMongoArticle>.Filter.Not(contentsFilterBase);
            
            var articles = collection.Find(Builders<ArchivedMongoArticle>.Filter.And(userFilter/*, doneFilter*/, contentsFilter)).ToList();

            if (articles.Count > 0)
            {
                foreach (var article in articles)
                {
                    Log.Info($"Updating concern level for article \"{article.ArticleId}\"...");
                    var concernAnalyzer = new ArticleConcernAnalyzer(article.GZipContents, settings.ConcernKeywords.ToArray());

                    article.ConcernLevel = concernAnalyzer.RetrieveConcern();
                    article.Done = true;
                    
                    article.GZipContents = new byte[0];   // reset it to save some space
                }
                updated_col.InsertMany(articles);
            }
            else
            {
                Log.Info("All concern levels seem to be updated!");
            }
        }
    }
}
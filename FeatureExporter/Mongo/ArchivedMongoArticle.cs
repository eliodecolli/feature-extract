using System;
using MongoDB.Bson;

namespace FeatureExporter.Mongo
{
    public class ArchivedMongoArticle
    {
        public ObjectId _id { get; set; }
        
        public string ArticleId { get; set; }
        
        public string Title { get; set; }
        
        public string Url { get; set; }
        
        public byte[] GZipContents { get; set; }
        
        public string User { get; set; }
        
        public bool Done { get; set; }
        
        
        public DateTime DatePublished { get; set; }
        
        public int ConcernLevel { get; set; }
    }
}
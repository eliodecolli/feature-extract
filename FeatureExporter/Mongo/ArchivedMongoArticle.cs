/*
 *    File Name:
 *         ArchivedMongoArticle.cs
 * 
 *    Purpose:
 *         General-purpose aggregate document. Represents an exported article on Mongo.
 *
 *     Author:
 *         Elio Decolli
 * 
 *     Last Updated:
 *         22/04/2020 - 10:34 PM
 */

using System;
using MongoDB.Bson;
using Newtonsoft.Json;

namespace FeatureExporter.Mongo
{
    public class ArchivedMongoArticle
    {
        [JsonIgnore]   // while playing locally with JSON.
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
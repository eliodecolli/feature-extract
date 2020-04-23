/*
 *    File Name:
 *         Settings.cs
 * 
 *    Purpose:
 *         Represents the global values needed throughout the domain of the application.
 *
 *     Author:
 *         Elio Decolli
 * 
 *     Last Updated:
 *         22/04/2020 - 10:34 PM
 */

using System.Collections.Generic;

namespace FeatureExporter
{
    public class Settings
    {
        /// <summary>
        /// API Key used in order to connect to New York Times endpoints.
        /// </summary>
        public string Key { get; set; }
        
        /// <summary>
        /// The number of pages to retrieve. Max is 10.
        /// </summary>
        public int TotalPages { get; set; }
        
        /// <summary>
        /// The initial page from which to start the data retrieval.
        /// </summary>
        public int StartPage { get; set; }
        
        /// <summary>
        /// Keywords used in order to collect the articles.
        /// </summary>
        public List<string> Keywords { get; set; }
        
        /// <summary>
        /// Cookies linked with a Http Request in order to access the articles form a user.
        /// </summary>
        public string Cookies { get; set; }
        
        /// <summary>
        /// Overwrite the cached articles metadata on disk.
        /// </summary>
        public bool CheckExisting { get; set; }
        
        /// <summary>
        /// The user performing the current data retrieval.
        /// </summary>
        public string User { get; set; }
        
        /// <summary>
        /// Keywords used to detect a concern level.
        /// </summary>
        public List<Keyword> ConcernKeywords { get; set; }
    }
}
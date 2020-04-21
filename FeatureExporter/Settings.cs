using System.Collections.Generic;

namespace FeatureExporter
{
    public class Settings
    {
        public string Key { get; set; }
        
        public int TotalPages { get; set; }
        
        public int StartPage { get; set; }
        
        public List<string> Keywords { get; set; }
        
        public string Cookies { get; set; }
        
        public bool CheckExisting { get; set; }
        
        public string User { get; set; }
        
        public List<Keyword> ConcernKeywords { get; set; }
    }
}
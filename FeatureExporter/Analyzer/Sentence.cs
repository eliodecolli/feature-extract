using System.Collections.Generic;

namespace FeatureExporter.Analyzer
{
    public class Sentence
    {
        public List<Token> Tokens { get; set; }
        
        public int ConcernLevel { get; set; }
    }
}
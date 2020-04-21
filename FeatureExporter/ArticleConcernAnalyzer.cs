using System.Collections.Generic;
using System.Linq;
using FeatureExporter.Analyzer;

namespace FeatureExporter
{

    public class Keyword
    {
        public string Value { get; set; }
        
        public List<string> Boosters { get; set; }
    }
    
    public class ArticleConcernAnalyzer
    {
        private CoreAnalyzer analyzer;

        public ArticleConcernAnalyzer(byte[] blob, Keyword[] keywords)
        {
            var boosters = new Dictionary<string, List<string>>();
            foreach (var keyword in keywords)
            {
                boosters.Add(keyword.Value, keyword.Boosters);
            }
            
            analyzer = new CoreAnalyzer(blob, keywords.Select(x => x.Value).ToList(), boosters);
        }

        public int RetrieveConcern()
        {
            var tokens = analyzer.Tokenize();
            analyzer.UpdateTokens(tokens);
            
            Log.Info($"Updating concern for {tokens.Length} tokens");

            int val = 0;
            foreach (var token in tokens)
            {
                val += token.ConcernLevel;
            }

            Log.Info($"Total concern level of {val}");
            return val;
        }
    }
}
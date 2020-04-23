/*
 *    File Name:
 *         ArticleConcernAnalyzer.cs
 * 
 *    Purpose:
 *         Makes use of a Core Analyzer to split the text.
 *         Abstracts away the internals and allows to implement additional builder configurations for an analysis. (Even though this is not the case here =\)
 *
 *     Author:
 *         Elio Decolli
 * 
 *     Last Updated:
 *         22/04/2020 - 10:56 PM
 */

using System.Collections.Generic;
using System.Linq;
using FeatureExporter.Analyzer;

namespace FeatureExporter
{
    /// <summary>
    /// A concern keyword.
    /// </summary>
    public class Keyword
    {
        /// <summary>
        /// Value of the keyword.
        /// </summary>
        public string Value { get; set; }
        
        /// <summary>
        /// Boosters associated with the keyword.
        /// </summary>
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
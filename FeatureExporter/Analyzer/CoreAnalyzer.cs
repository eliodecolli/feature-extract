/*
 *    File Name:
 *         CoreAnalyzer.cs
 * 
 *    Purpose:
 *         Perform a tokenization using Open NLP and analyze the current concern level of a text based on a given list of keywords.
 *
 *     Author:
 *         Elio Decolli
 * 
 *     Last Updated:
 *         22/04/2020 - 10:34 PM
 *
 *     TODO:
 *         [] Use a standard naming convention for variables.
 *         [] Assign each booster a "power".
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using OpenNLP.Tools.SentenceDetect;
using OpenNLP.Tools.Tokenize;

namespace FeatureExporter.Analyzer
{
    public class CoreAnalyzer
    {
        private string blob;

        private List<string> keywords;

        private Dictionary<string, List<string>> boosters;
        
        private static EnglishMaximumEntropySentenceDetector sd = new EnglishMaximumEntropySentenceDetector(Path.Combine(Environment.CurrentDirectory, "Model", "EnglishSD.nbin"));

        public CoreAnalyzer(byte[] gzippedBlob, List<string> keywords, Dictionary<string, List<string>> boosters)
        {
            using var ccmem = new MemoryStream(gzippedBlob);
            using var nstream = new MemoryStream();
            using var gzip = new GZipStream(ccmem, CompressionMode.Decompress);
            
            gzip.CopyTo(nstream);
            blob = Encoding.UTF8.GetString(nstream.ToArray());
            
            this.keywords = keywords;
            this.boosters = boosters;
        }

        /// <summary>
        /// Split the text into sentences.
        /// </summary>
        public Sentence[] Tokenize()
        {
            var matches = sd.SentenceDetect(blob);
            
            var retval = new List<Sentence>();
            
            var sb = new StringBuilder();
            
            foreach (var match in matches)
            {
                sb.AppendLine(match);
                
                var sentence = new Sentence()
                {
                    Tokens = new List<Token>(),
                    ConcernLevel = 0
                };
                var tokenizer = new EnglishRuleBasedTokenizer(false);
                var words = tokenizer.Tokenize(match);
                
                foreach (var word in words)
                {
                    var token = new Token()
                    {
                        Value = word,
                        SentenceIndex = -1
                    };
                    sentence.Tokens.Add(token);
                }
                retval.Add(sentence);
            }

            return retval.ToArray();
        }

        /// <summary>
        /// Analyzes the given sentences and assigns them a concern level.
        /// </summary>
        /// <param name="tokens">The sentences which make up a text.</param>
        public void UpdateTokens(Sentence[] tokens)
        {
            foreach (var sentence in tokens)
            {
                var currentBoost = 1.0;
                var currentConcern = 0.0;

                var tempBoosters = new List<string>();

                foreach (var token in sentence.Tokens)
                {
                    if (keywords.Contains(token.Value.ToLower()))
                    {
                        currentConcern += currentBoost;
                        
                        if(boosters.ContainsKey(token.Value.ToLower())) tempBoosters.AddRange(boosters[token.Value.ToLower()]);
                    }

                    if (tempBoosters.Contains(token.Value.ToLower()))
                    {
                        currentBoost *= (currentBoost / 2.0);   // this is gonna get exponentially bigger
                        currentConcern += currentBoost;
                    }
                }

                sentence.ConcernLevel = (int) Math.Ceiling(currentConcern);
            }
        }
    }
}
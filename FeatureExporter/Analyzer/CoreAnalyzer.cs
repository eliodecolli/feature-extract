using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using MongoDB.Bson.Serialization.Serializers;

namespace FeatureExporter.Analyzer
{
    public class CoreAnalyzer
    {
        private string blob;

        private List<string> keywords;

        private Dictionary<string, List<string>> boosters;

        public CoreAnalyzer(byte[] gzippedBlob, List<string> keywords, Dictionary<string, List<string>> boosters)
        {
            using var ccmem = new MemoryStream(gzippedBlob);
            using var nstream = new MemoryStream();
            using var gzip = new GZipStream(ccmem, CompressionMode.Decompress);
            
            gzip.CopyTo(nstream);
            blob = Encoding.ASCII.GetString(nstream.ToArray());
            
            File.WriteAllText("lala.txt", blob);

            this.keywords = keywords;
            this.boosters = boosters;
        }

        public Sentence[] Tokenize()
        {
            var matches = Regex.Matches("blob", @"[A-Za-z](.*?|\n?|\r?)*?[.?!]+(?=\W)");
            var retval = new List<Sentence>();
            
            foreach (Match match in matches)
            {
                var sentence = new Sentence()
                {
                    Tokens = new List<Token>(),
                    ConcernLevel = 0
                };
                foreach (var word in match.Value.Split(" "))
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
                Log.Info($"Concern: +{sentence.ConcernLevel}");
            }
        }
    }
}
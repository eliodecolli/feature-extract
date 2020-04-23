/*
 *    File Name:
 *         Sentence.cs
 * 
 *    Purpose:
 *         Represents a sentence inside an article.
 *
 *     Author:
 *         Elio Decolli
 * 
 *     Last Updated:
 *         22/04/2020 - 10:34 PM
 */

using System.Collections.Generic;

namespace FeatureExporter.Analyzer
{
    public class Sentence
    {
        public List<Token> Tokens { get; set; }
        
        public int ConcernLevel { get; set; }
    }
}
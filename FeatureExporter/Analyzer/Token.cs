/*
 *    File Name:
 *         Token.cs
 * 
 *    Purpose:
 *         Represents a token (word) of a text (sentence).
 *
 *     Author:
 *         Elio Decolli
 * 
 *     Last Updated:
 *         22/04/2020 - 10:34 PM
 */

namespace FeatureExporter.Analyzer
{
    public class Token
    {
        public string Value { get; set; }
        
        public int SentenceIndex { get; set; }
    }
}
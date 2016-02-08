using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Globalization;
using Shaman.Runtime;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
#if !STANDALONE && !SALTARELLE
using NTextCat;
using Shaman.Runtime.Storage;
#if !SALTARELLE
using Newtonsoft.Json;
#endif
#endif

#if SALTARELLE
using System.Text.Saltarelle;
#else
using System.Text;
#endif

namespace Shaman
{
    /// <summary>
    /// Provides text manipulation functions.
    /// </summary>
#if SALTARELLE
    public static partial class Utils
#else
    public static class StringUtils
#endif
    {

        public static double UpperCaseLettersProportion(string text)
        {
            var allLower = text.ToLowerFast();
            var allUpper = text.ToUpper();
            var upperCount = 0;
            var lettersCount = 0;
            for (var i = 0; i < text.Length; i++)
            {
                if ((allLower[i] == allUpper[i]))
                {
                    continue;
                }
                lettersCount++;
                if ((allUpper[i] == text[i]))
                {
                    upperCount++;
                }
            }
            if ((lettersCount == 0))
            {
                return 0;
            }
            return ((double)upperCount / lettersCount);
        }

        public static string ToTitleCase(this string text)
        {
            var allLower = text.ToLowerFast();
            var allUpper = text.ToUpper();
            var result = ReseekableStringBuilder.AcquirePooledStringBuilder();
            var firstLetter = true;
            for (int i = 0; i < text.Length; i++)
            {
                if (firstLetter)
                {
                    result.Append(allUpper[i]);
                    firstLetter = false;
                }
                else
                {
                    result.Append(allLower[i]);
                }
                if (text[i] == ' ') firstLetter = true;
            }

            return ReseekableStringBuilder.GetValueAndRelease(result);
        }


        public static string ToPascalCase(this string text, bool allowDot)
        {
            var allLower = text.ToLowerFast();
            var allUpper = text.ToUpper();
            var result = ReseekableStringBuilder.AcquirePooledStringBuilder();
            var firstLetter = true;
            for (int i = 0; i < text.Length; i++)
            {
                if (allowDot && text[i] == '.')
                {
                    result.Append(allLower[i]);
                    firstLetter = true;
                }
#if SALTARELLE
                else if (AwdeeUtils.IsLetterOrDigit(text[i]))
#else
                else if (char.IsLetterOrDigit(text[i]))
#endif
                {
                    if (firstLetter)
                    {
                        result.Append(allUpper[i]);
                        firstLetter = false;
                    }
                    else
                    {
                        result.Append(text[i]);
                    }
                }
                else
                {
                    firstLetter = true;
                }
            }

            return ReseekableStringBuilder.GetValueAndRelease(result);
        }


        public static string ToPascalCase(this string text)
        {
            return ToPascalCase(text, false);
        }

        public static string PascalCaseToCamelCase(this string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            return char.ToLower(text[0]).ToString() + text.Substring(1);
        }

        public static string PascalCaseToNormalCase(this string text)
        {
#if SALTARELLE
            var sb = new StringBuilder();
#else
            var sb = ReseekableStringBuilder.AcquirePooledStringBuilder();
#endif
            for (int i = 0; i < text.Length; i++)
            {
                var ch = text[i];
                if (char.IsUpper(ch) && i != 0)
                {
                    sb.Append(' ');
#if SALTARELLE
                    sb.Append(ch.ToString().ToLower());
#else
                    sb.Append(char.ToLower(ch));
#endif
                }
                else if (ch == '.')
                {
                    // Ignore
                }
                else
                {
                    sb.Append(ch);
                }
            }

#if SALTARELLE
            return sb.ToString();
#else
            return ReseekableStringBuilder.GetValueAndRelease(sb);
#endif
        }



        private static string[] SpaceArray = new[] { " " };

        public static List<string> GetDistinctWords(string text, bool removeAccentMarks)
        {
            return GetWords(text, removeAccentMarks).Distinct().ToList();
        }

#if !STANDALONE && !SALTARELLE

        [Configuration]
        private static int Configuration_NCatTextMaxNGramLength = 5;

        [Configuration]
        private static int Configuration_NCatTextMaxDistributionSize = 4000;

        [Configuration]
        private static int Configuration_NCatTextOccuranceNumberThreshold = 0;


        [Configuration]
        public static string Configuration_LanguageModelsPath = "Awdee2.Declarative/LanguageModels.dat";

        private static RankedLanguageIdentifier _languageIdentifier;
        internal static RankedLanguageIdentifier LanguageIdentifier
        {
            get
            {
                if (_languageIdentifier == null)
                {

                    var factory = new NTextCat.RankedLanguageIdentifierFactory(
                        Configuration_NCatTextMaxNGramLength,
                        Configuration_NCatTextMaxDistributionSize,
                        Configuration_NCatTextOccuranceNumberThreshold,
                        int.MaxValue,
                        false);
                    
                    using (var stream = File.Open(ConfigurationManager.CombineRepositoryOrEntrypointPath(Configuration_LanguageModelsPath), FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete))
                    {

                        _languageIdentifier = factory.LoadBinary(stream);
                    }


                }
                return _languageIdentifier;
            }
        }

        public static string DetectLanguage(string text)
        {
            return FullTextIndexing.ThreeToTwoLetterLanguageCode(LanguageIdentifier.Identify(text).FirstOrDefault()?.Item1.Iso639_2T);
        }
#endif


#if WINDOWS_PHONE || SALTARELLE || NETFX_CORE
        private static readonly string Accents = "ßàáâãäåæçèéêëìíîïòóôõöùúûüýăąćčďđēęěğİĺľłńňőœŕřśşšţťůűźżžșțΐάέήίΰϊϋόύώъїѝґابتحدسصطعكويَّ";
        private static readonly string Removed = "saaaaaaaceeeeiiiiooooouuuuyaaccddeeegilllnnoorrsssttuuzzzstιαεηιυιυουωьiиrءپثخذشضظغخؤئٰـ";
#endif



        public static string RemoveAccentMarksAndToLower(string text)
        {

            var hasUppers = false;
            var hasHigh = false;
            for (int i = 0; i < text.Length; i++)
            {
                var ch = text[i];
                if (ch >= '\xC0' /* À */) { hasHigh = true; break; }
                if (ch >= 'A' && ch <= 'Z') hasUppers = true;
            }

            if (!hasHigh && !hasUppers) return text;
            if (!hasHigh) return text.ToLowerFast();

#if SALTARELLE || NETFX_CORE
            var sb = new StringBuilder();
            for (int i = 0; i < text.Length; i++)
            {
                var ch = text[i];
#if SALTARELLE
                ch = ch.ToString().ToLower()[0];
#else
                if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark) continue;
                ch = char.ToLowerInvariant(ch);
#endif
                var idx = Accents.IndexOf(ch);
                if(idx!=-1) ch = Removed[idx];
                sb.Append(ch);
            }
            return sb.ToString();
 



#else
            //var tempBytes = Encoding.GetEncoding("latin2").GetBytes(text);
            //return Encoding.UTF8.GetString(tempBytes, 0, tempBytes.Length).ToLower();
            //#else
            string normalizedString = text.Normalize(NormalizationForm.FormD);
            StringBuilder stringBuilder = ReseekableStringBuilder.AcquirePooledStringBuilder();
            for (int i = 0; i < normalizedString.Length; i++)
            {
                char c = normalizedString[i];
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }
            return ReseekableStringBuilder.GetValueAndRelease(stringBuilder).ToLowerFast();
#endif
        }


#if !SALTARELLE
        public static bool MatchesAllWords(string text,
#if NET35
        IList<string>
#else
        IReadOnlyList<string>
#endif
         wordsNoDiacritics)
        {
            if (wordsNoDiacritics.Count == 0) return true;
            if (text == null) return false;
            var w = GetWords(text, true);
            foreach (var item in wordsNoDiacritics)
            {
                if (!w.Contains(item)) return false;
            }
            return true;
        }
#endif

        public static List<string> GetWords(string text, bool removeAccentMarks)
        {
            return GetWords(text, removeAccentMarks, false);
        }

        public static List<string> GetWords(string text, bool removeAccentMarks, bool preserveCase)
        {
            if (preserveCase && removeAccentMarks) throw new ArgumentException();
            string str = preserveCase ? text : removeAccentMarks ? RemoveAccentMarksAndToLower(text) : text.ToLowerFast();

            var words = new List<string>();
            StringBuilder currentWord = null;
            bool prevCharWasSep = true;
            for (int i = 0; i < str.Length; i++)
            {
                bool curCharIsSep;
                char c = str[i];

#if SALTARELLE
                var whitespace = c == ' ' || (c >= '\t' && c <= '\r') || c == '\u00a0' || c == '\u0085'; 
                if(!whitespace)
#else
                if (char.IsLetter(c) || char.IsDigit(c) || CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
#endif
                {
                    if (prevCharWasSep)
                    {
#if SALTARELLE
                        currentWord = new StringBuilder();
#else
                        currentWord = ReseekableStringBuilder.AcquirePooledStringBuilder();
#endif
                        prevCharWasSep = false;
                    }
                    currentWord.Append(c);
#if SALTARELLE
                    curCharIsSep =
                        (c >= 0x2E80 && c < 0x2E00) ||
                        (c >= 0x3400 && c < 0xA500) ||
                        (c >= 0xAC00 && c < 0xD800) ||
                        (c >= 0xF900 && c < 0xFB00);
#else
                    curCharIsSep = CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.OtherLetter;
#endif
                }
                else
                {
                    curCharIsSep = true;
                }
                if (curCharIsSep)
                {
                    if (currentWord != null)
                    {
                        words.Add(currentWord.ToString());
#if !SALTARELLE
                        ReseekableStringBuilder.Release(currentWord);
#endif
                        currentWord = null;
                    }
                    prevCharWasSep = true;
                }
            }
            if (currentWord != null)
            {
                words.Add(currentWord.ToString());
#if !SALTARELLE
                ReseekableStringBuilder.Release(currentWord);
#endif
            }

            return words;

        }



        public static bool ContainsAllWords(string text, IEnumerable<string> requiredWords, bool removeAccentMarks)
        {
            var itemWords = GetWords(text, removeAccentMarks);
            return requiredWords.All(x => itemWords.Contains(x));
        }

        public static Func<string, bool> ParseSearchQuery(string query)
        {
            if (query == null) return s => true;

            var requiredWords = GetWords(query, true);
            return item =>
            {
                var itemWords = GetWords(item, true);
                return requiredWords.All(x => itemWords.Any(y => y.StartsWith(x)));
            };
        }




        public static bool ContainsNonLatinCharacters(string text)
        {
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
#if SALTARELLE
                var p = (int)c;
                if (p < 256) continue;
                if (p >= 0x0370 && p < 0x1E00) return true;
                if (p >= 0x1F00 && p < 0x2000) return true;
                if (p >= 0x2C00 && p < 0xD800) return true;
                if (p >= 0xF900 && p < 0x10000) return true;
#else
                if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.OtherLetter) return true;
#endif
            }
            return false;
        }

        public static string ToTitleCaseIfNeeded(this string text)
        {
            var proportion = UpperCaseLettersProportion(text);
            if (proportion == 0 || proportion >= Configuration_MinUpperCaseLetterRatioForToTitleCaseIfNeeded)
            {
                return text.ToTitleCase();
            }
            return text;
        }

#if !STANDALONE && !SALTARELLE
        [Configuration]
#endif
        private readonly static double Configuration_MinUpperCaseLetterRatioForToTitleCaseIfNeeded = 0.3;




#if NETFX_CORE


#endif

    }




}



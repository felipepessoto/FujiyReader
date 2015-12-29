using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace EasyPocket.Core
{
    public class Html2Article
    {
        private static readonly string[][] Filters =
        {
            new[] { @"(?is)<script.*?>.*?</script>", "" },
            new[] { @"(?is)<style.*?>.*?</style>", "" },
            new[] { @"(?is)<!--.*?-->", "" },
            
            new[] { @"(?is)</a>", "</a>\n"}
        };

        private static bool _appendMode = false;

        public static bool AppendMode
        {
            get { return _appendMode; }
            set { _appendMode = value; }
        }

        private static int _depth = 6;
        
        public static int Depth
        {
            get { return _depth; }
            set { _depth = value; }
        }

        private static int _limitCount = 180;
        
        public static int LimitCount
        {
            get { return _limitCount; }
            set { _limitCount = value; }
        }

        private static int _headEmptyLines = 2;
        private static int _endLimitCharCount = 20;


        public static Article GetArticle(string html)
        {
            if (html.Count(c => c == '\n') < 10)
            {
                html = html.Replace(">", ">\n");
            }

            string body = "";
            string bodyFilter = @"(?is)<body.*?</body>";
            Match m = Regex.Match(html, bodyFilter);
            if (m.Success)
            {
                body = m.ToString();
            }
            
            foreach (var filter in Filters)
            {
                body = Regex.Replace(body, filter[0], filter[1]);
            }

            body = Regex.Replace(body, @"(<[^<>]+)\s*\n\s*", FormatTag);

            string content;
            string contentWithTags;
            GetContent(body, out content, out contentWithTags);

            Article article = new Article
            {
                Title = GetTitle(html),
                PublishDate = GetPublishDate(body),
                Content = content,
                ContentWithTags = contentWithTags
            };

            return article;
        }

        private static string FormatTag(Match match)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var ch in match.Value)
            {
                if (ch == '\r' || ch == '\n')
                {
                    continue;
                }
                sb.Append(ch);
            }
            return sb.ToString();
        }
        
        private static string GetTitle(string html)
        {
            string titleFilter = @"<title>[\s\S]*?</title>";
            string h1Filter = @"<h1.*?>.*?</h1>";
            string clearFilter = @"<.*?>";

            string title = "";
            Match match = Regex.Match(html, titleFilter, RegexOptions.IgnoreCase);
            if (match.Success)
            {
                title = Regex.Replace(match.Groups[0].Value, clearFilter, "");
            }

            match = Regex.Match(html, h1Filter, RegexOptions.IgnoreCase);
            if (match.Success)
            {
                string h1 = Regex.Replace(match.Groups[0].Value, clearFilter, "");
                if (!String.IsNullOrEmpty(h1) && title.StartsWith(h1))
                {
                    title = h1;
                }
            }
            return title;
        }

        private static DateTime GetPublishDate(string html)
        {
            
            string text = Regex.Replace(html, "(?is)<.*?>", "");
            Match match = Regex.Match(
                text,
                @"((\d{4}|\d{2})(\-|\/)\d{1,2}\3\d{1,2})(\s?\d{2}:\d{2})?|(\d{4}年\d{1,2}月\d{1,2}日)(\s?\d{2}:\d{2})?",
                RegexOptions.IgnoreCase);

            DateTime result = new DateTime(1900, 1, 1);
            if (match.Success)
            {
                string dateStr = "";
                for (int i = 0; i < match.Groups.Count; i++)
                {
                    dateStr = match.Groups[i].Value;
                    if (!String.IsNullOrEmpty(dateStr))
                    {
                        break;
                    }
                }
                
                if (dateStr.Contains("年"))
                {
                    StringBuilder sb = new StringBuilder();
                    foreach (var ch in dateStr)
                    {
                        if (ch == '年' || ch == '月')
                        {
                            sb.Append("/");
                            continue;
                        }
                        if (ch == '日')
                        {
                            sb.Append(' ');
                            continue;
                        }
                        sb.Append(ch);
                    }
                    dateStr = sb.ToString();
                }
                result = Convert.ToDateTime(dateStr);

                if (result.Year < 1900)
                {
                    result = new DateTime(1900, 1, 1);
                }
            }
            return result;
        }

        private static void GetContent(string bodyText, out string content, out string contentWithTags)
        {
            string[] orgLines = null;
            string[] lines = null;

            orgLines = bodyText.Split('\n');
            lines = new string[orgLines.Length];
            
            for (int i = 0; i < orgLines.Length; i++)
            {
                string lineInfo = orgLines[i];
                
                lineInfo = Regex.Replace(lineInfo, "(?is)</p>|<br.*?/>", "[crlf]");
                lines[i] = Regex.Replace(lineInfo, "(?is)<.*?>", "").Trim();
            }

            StringBuilder sb = new StringBuilder();
            StringBuilder orgSb = new StringBuilder();

            int preTextLen = 0;
            int startPos = -1;
            for (int i = 0; i < lines.Length - _depth; i++)
            {
                int len = 0;
                for (int j = 0; j < _depth; j++)
                {
                    len += lines[i + j].Length;
                }

                if (startPos == -1)
                {
                    if (preTextLen > _limitCount && len > 0)
                    {
                        int emptyCount = 0;
                        for (int j = i - 1; j > 0; j--)
                        {
                            if (String.IsNullOrEmpty(lines[j]))
                            {
                                emptyCount++;
                            }
                            else
                            {
                                emptyCount = 0;
                            }
                            if (emptyCount == _headEmptyLines)
                            {
                                startPos = j + _headEmptyLines;
                                break;
                            }
                        }
                        
                        if (startPos == -1)
                        {
                            startPos = i;
                        }
                        
                        for (int j = startPos; j <= i; j++)
                        {
                            sb.Append(lines[j]);
                            orgSb.Append(orgLines[j]);
                        }
                    }
                }
                else
                {
                    if (len <= _endLimitCharCount && preTextLen < _endLimitCharCount)
                    {
                        if (!_appendMode)
                        {
                            break;
                        }
                        startPos = -1;
                    }
                    sb.Append(lines[i]);
                    orgSb.Append(orgLines[i]);
                }
                preTextLen = len;
            }

            string result = sb.ToString();
            
            content = result.Replace("[crlf]", Environment.NewLine);
            content = WebUtility.HtmlDecode(content);
            
            contentWithTags = orgSb.ToString();
        }
    }

    public class Article
    {
        public string Title { get; set; }
        
        public string Content { get; set; }
        
        public string ContentWithTags { get; set; }
        
        public DateTime PublishDate { get; set; }
    }
}

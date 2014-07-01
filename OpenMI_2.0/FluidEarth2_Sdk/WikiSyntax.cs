
using System;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using System.Text;
using System.Xml.Linq;

namespace FluidEarth2.Sdk
{
    public class WikiSyntax
    {
        string _title;
        string _syntax = string.Empty;
        Convertor _convertor = new Convertor();

        public WikiSyntax(string title, string wikiText)
        {
            _title = title;
            _syntax = wikiText;
        }

        public WikiSyntax(string title, XElement element)
        {
            _title = title;

            if (element == null
                || (element.Attribute("cdata_format") != null
                    && element.Attribute("cdata_format").Value != "wikisyntax"))
                return;

            StringBuilder s = new StringBuilder();

            foreach (XNode node in element.Nodes())
                if (node.NodeType == System.Xml.XmlNodeType.CDATA)
                    s.Append(((XCData)node).Value);

            StringBuilder sb = new StringBuilder();

            foreach (string line in s.ToString().Split('\n'))
                sb.AppendLine(line.TrimEnd());

            _syntax = sb.ToString().Trim(new char[] { '\r', '\n' });
        }

        static XElement NewElement(string element, string cdata)
        {
            return new XElement(element,
                new XAttribute("cdata_format", "wikisyntax"),
                new XComment("User added notes"),
                new XComment("Attibute cdata_format defaults to wikisyntax"),
                new XCData(cdata));
        }

        public XElement Element
        {
            get 
            {
                return _syntax != string.Empty
                    ? NewElement(_title, string.Format("\r\n{0}\r\n", _syntax))
                    : null;
            }
        }

        public string Title
        {
            get { return _title; }
        }

        public string Syntax
        {
            get { return _syntax; }
            set { _syntax = value; }
        }

        public string Html(string title)
        {
            return Html(title, null);
        }

        public string Html(string title, DirectoryInfo htmlFolder)
        {
            var css = Utilities.LocalPath("Pipistrelle.css", null);

            var links = new List<XElement>();

            links.Add(new XElement("link",
                new XAttribute("rel", "stylesheet"),
                new XAttribute("href", css),
                new XAttribute("type", "text/css")
                ));

            links.Add(new XElement("link",
                new XAttribute("rel", "alternate stylesheet"),
                new XAttribute("href", @"https://FluidEarth2.svn.sourceforge.net/viewvc/fluidearth/trunk/FluidEarth2_Pipistrelle/Pipistrelle.css"),
                new XAttribute("type", "text/css")
                ));

            return _convertor.ToHtml(title, links, _syntax, htmlFolder);
        }

        public static string HtmlContentUnavailable(string title, string css, string comment)
        {
            return Html(title, css, new XElement[] { new XElement("p", comment) });
        }

        public static string Html(string title, string css, XElement[] elements)
        {
            return new XDocument(
                new XDocumentType("HTML", "-//W3C//DTD HTML 4.0 Transitional//EN", null, null),
                new XElement("html",
                    new XElement("head",
                        new XElement("title", title != null ? title : "Unknown"),
                        new XElement("link",
                            new XAttribute("rel", "stylesheet"),
                            new XAttribute("href", css != null ? css : "Unknown.css"),
                            new XAttribute("type", "text/css")
                            )
                        ),
                    new XElement("body", elements)
                    )
                ).ToString();
        }
    }

    public class Convertor
    {
        List<StringBuilder> _body = new List<StringBuilder>();
        List<StringBuilder> _paragraphs = new List<StringBuilder>();
        bool _inParagraph = false;
        StringBuilder _paragraph = null;
        
        int _unorderedDepth = 0;
        string[] _unordered = new string[] {
                "*", "**", "***", "****", "*****", "******", "*******",
            };

        int _orderedDepth = 0;
        string[] _ordered = new string[] {
                "#", "##", "###", "####", "#####", "######", "#######",
            };

        char[] _endWord = new char[] { 
            ' ', '(', ',', '.', '?', '!', ':', ';', '"', '\'', ')', };

        bool _preformatted;

        bool _inTable;
        int _tableRowCount;

        public string ToHtml(string title, List<XElement> links, string wikiSyntax)
        {
            return ToHtml(title, links, wikiSyntax, null);
        }

        public string ToHtml(string title, List<XElement> links, string wikiSyntax, DirectoryInfo htmlFolder)
        {
            return Html(title, links, Body(wikiSyntax, htmlFolder));
        }

        string Html(string title, List<XElement> links, string body)
        {
            string html = new XDocument(
                new XDocumentType("HTML", "-//W3C//DTD HTML 4.0 Transitional//EN", null, null),
                new XElement("html",
                    new XElement("head",
                        new XElement("title", title), links),
                    new XElement("body", "~~BODY~~")
                    )
                ).ToString();

            return html.Replace("~~BODY~~", body);
        }

        void EndParagraph()
        {
            if (!_inParagraph)
                return;

            _paragraph = ReplaceHtmlEscapes(_paragraph);

            _paragraphs.Add(_paragraph);
            _inParagraph = false;

            if (_paragraph.Length > 0)
            {
                if (_inTable || _orderedDepth > 0 || _unorderedDepth > 0)
                    _body.Add(_paragraph);
                else
                {                    
                    _body.Add(new StringBuilder("<p>"));
                    _body.Add(_paragraph);
                    _body.Add(new StringBuilder("</p>"));              
                }
            }
        }

        void NewParagraph()
        {
            EndParagraph();

            _paragraph = new StringBuilder();
            _inParagraph = true;
        }

        void EndUnorderedList()
        {
            if (_unorderedDepth < 1)
                return;

            EndParagraph();

            StringBuilder sb = new StringBuilder();

            while (_unorderedDepth-- > 0)
                sb.Append("</li></ul>");

            _unorderedDepth = 0;

            _body.Add(sb);
        }

        void EndOrderedList()
        {
            if (_orderedDepth < 1)
                return;

            EndParagraph();

            StringBuilder sb = new StringBuilder();

            while (_orderedDepth-- > 0)
                sb.Append("</li></ol>");

            _orderedDepth = 0;

            _body.Add(sb);
        }

        string Body(string wikiSyntax)
        {
            return Body(wikiSyntax, null);
        }

        string Body(string wikiSyntax, DirectoryInfo htmlfolder)
        {
            StringBuilder sb = new StringBuilder();

            _body.Clear();
            _paragraphs.Clear();

            StringBuilder header = null;

            _paragraph = null;
            _inParagraph = false;
            _preformatted = false;

            _unorderedDepth = 0;
            _orderedDepth = 0;

            _inTable = false;
            _tableRowCount = 0;

            string line;

            foreach (string l in wikiSyntax.Split('\n'))
            {
                if (_preformatted)
                {
                    if (l.StartsWith("}}}"))
                    {
                        _body.Add(new StringBuilder("</pre>"));
                        _preformatted = false;
                    }
                    else
                        _body.Add(ReplaceHtmlEscapes(new StringBuilder(l)));

                    continue;
                }

                line = l.TrimEnd('\r').Trim();

                if ((header = Header("=====", line)) != null
                    || (header = Header("====", line)) != null
                    || (header = Header("===", line)) != null
                    || (header = Header("==", line)) != null
                    || (header = Header("=", line)) != null
                    )
                {
                    EndParagraph();
                    EndUnorderedList();
                    EndOrderedList();
                    _body.Add(header);
                    continue;
                }

                if (line == "----") // Horizontal rule
                {
                    EndParagraph();
                    EndUnorderedList();
                    EndOrderedList();
                    _body.Add(new StringBuilder("<hr />"));
                    continue;
                }

                if (line == "{{{") // Preformatted
                {
                    EndParagraph();
                    EndUnorderedList();
                    EndOrderedList();
                    _body.Add(new StringBuilder("<pre>"));
                    _preformatted = true;
                    continue;
                }

                line = Lists(line, "ul", '*', ref _unorderedDepth);
                line = Lists(line, "ol", '#', ref _orderedDepth);

                if (line.StartsWith("|")) // Table
                {
                    EndParagraph();
                    EndUnorderedList();
                    EndOrderedList();

                    if (!_inTable)
                    {
                        _body.Add(new StringBuilder("<table>"));
                        _inTable = true;
                    }
                }

                if (line.Length > 0)
                {
                    if (!_inParagraph)
                        NewParagraph();

                    _paragraph.Append(" " + line);
                }
                else if (_inParagraph)
                {
                    EndParagraph();
                    EndUnorderedList();
                    EndOrderedList();
                }

                if (_inTable && !line.StartsWith("|"))
                {
                    _inTable = false;
                    _tableRowCount = 0;
                    EndParagraph();
                    _body.Add(new StringBuilder("</table>"));
                }
            }

            EndParagraph();
            EndUnorderedList();
            EndOrderedList();
            
            List<string> pre = Extract("{{{", "}}}", "<tt>", "</tt>");
            List<string> links = Extract("[[", "]]", "<a>", "</a>");
            links.AddRange(Extract("http://", _endWord, "<a>", "</a>"));
            links.AddRange(Extract("ftp://", _endWord, "<a>", "</a>"));
            List<string> images = Extract("{{", "}}", "<img>", "</img>");

            Substitute(new string[] { "**" }, "<strong>", "</strong>");
            Substitute(new string[] { "//" }, "<em>", "</em>");
            Substitute(new string[] { "\\\\" }, "<br />");
            SubstituteTables(); 
            SubstituteEscapes();

            ReplaceLinks(links);
            ReplaceImages(images, htmlfolder); 
            Replace("<tt>", "</tt>", pre);

            foreach (StringBuilder b in _body)
            {
#if _DEBUG_
                String s = b.ToString();

                if (s.StartsWith("<p") 
                    || s.StartsWith("<h") 
                    || s.StartsWith("<table") 
                    || s.StartsWith("<tr")
                    || s.StartsWith("<ul><li>")
                    || s.StartsWith("<ol><li>")
                    )
                    sb.AppendLine();

                sb.Append(s);
#else
                sb.Append(b.ToString());
#endif
            }

            return sb.ToString();
        }

        string Lists(string line, string element, char key, ref int depth)
        {
            int keyCount = 0;

            for (; keyCount < line.Length; ++keyCount)
                if (line[keyCount] != key)
                    break;

            if (keyCount < 1)
                return line;

            EndParagraph();

            if (line.Length < 1) // End list
            {
                if (depth > 0)
                {
                    StringBuilder sb = new StringBuilder();

                    while (--depth > 0)
                        sb.Append(string.Format("</li></{0}>", element));
                }

                return line;
            }

            // New item

            if (keyCount < depth)
            {
                string end = string.Format("</li></{0}>", element);

                StringBuilder sb2 = new StringBuilder(end);

                while (keyCount < --depth)
                    sb2.Append(end);

                _body.Add(sb2);
            }

            if (depth == 0) // start list
            {
                depth = 1;
                _body.Add(new StringBuilder(string.Format("<{0}><li>", element)));
            }
            else if (line.Length > depth && line[depth] == key) // sub item
            {
                ++depth;
                _body.Add(new StringBuilder(string.Format("<{0}><li>", element)));
            }
            else // next item
                _body.Add(new StringBuilder("</li><li>"));

            return line.Substring(depth).Trim();
        }

        bool Found(int nStart, string text)
        {
            if (nStart > 0 && text[nStart - 1] == '~')
                return false;

            return nStart > -1;
        }

        void Replace(string start, string end, List<string> values)
        {
            int nStart, nEnd, index;
            string para;

            foreach (StringBuilder paragraph in _paragraphs)
            {
                nStart = -1;

                do
                {
                    para = paragraph.ToString();
                    nStart = para.IndexOf(start, nStart + 1);

                    if (!Found(nStart, para))
                    {
                        continue;
                    }

                    nEnd = para.IndexOf(end, nStart);

                    if (nEnd < 0)
                        continue;

                    index = int.Parse(para.Substring(nStart + start.Length, nEnd - nStart - start.Length));

                    if (index >= values.Count)
                        continue;

                    paragraph.Remove(nStart, nEnd + end.Length - nStart);
                    paragraph.Insert(nStart, string.Format("{0}{1}{2}", start, values[index], end));
                }
                while (nStart > 0);
            }
        }

        static StringBuilder ReplaceHtmlEscapes(StringBuilder sb)
        {
            sb = sb.Replace("\"", "&quot;");
            sb = sb.Replace("<", "&lt;");
            sb = sb.Replace(">", "&gt;"); 
            return sb;
        }

        void ReplaceLinks(List<string> values)
        {
            int nStart, nEnd, index, nBar;
            string para, href, caption;

            foreach (StringBuilder paragraph in _paragraphs)
            {
                nStart = -1;

                do
                {
                    para = paragraph.ToString();
                    nStart = para.IndexOf("<a>", nStart + 1);

                    if (!Found(nStart, para))
                        continue;

                    nEnd = para.IndexOf("</a>", nStart);

                    if (nEnd < 0)
                        continue;

                    index = int.Parse(para.Substring(nStart + 3, nEnd - nStart - 3));

                    if (index >= values.Count)
                        continue;

                    nBar = values[index].IndexOf("|", 0);

                    if (nBar < 0)
                    {
                        href = values[index];
                        caption = values[index];
                    }
                    else
                    {
                        href = values[index].Substring(0, nBar);
                        caption = values[index].Substring(nBar + 1);
                    }

                    paragraph.Remove(nStart, nEnd + 4 - nStart);
                    paragraph.Insert(nStart, string.Format("<a href=\"{0}\">{1}</a>",
                        href, caption));                            
                }
                while (nStart > 0);
            }
        }

        void ReplaceImages(List<string> values)
        {
            ReplaceImages(values, null);
        }

        void ReplaceImages(List<string> values, DirectoryInfo htmlfolder)
        {
            int nStart, nEnd, index, nBar;
            string para, src, alt;

            foreach (StringBuilder paragraph in _paragraphs)
            {
                nStart = -1;

                do
                {
                    para = paragraph.ToString();
                    nStart = para.IndexOf("<img>", nStart + 1);

                    if (!Found(nStart, para))
                        continue;

                    nEnd = para.IndexOf("</img>", nStart);

                    if (nEnd < 0)
                        continue;

                    index = int.Parse(para.Substring(nStart + 5, nEnd - nStart - 5));

                    if (index >= values.Count)
                        continue;

                    nBar = values[index].IndexOf("|", 0);

                    if (nBar < 0)
                    {
                        src = values[index];
                        alt = values[index];
                    }
                    else
                    {
                        src = values[index].Substring(0, nBar);
                        alt = values[index].Substring(nBar + 1);
                    }

                    if (!Path.IsPathRooted(src) && htmlfolder != null && File.Exists(Path.Combine(htmlfolder.FullName, src)))
                        src = Path.Combine(htmlfolder.FullName, src);

                    paragraph.Remove(nStart, nEnd + 6 - nStart);
                    paragraph.Insert(nStart, string.Format("<img src=\"{0}\" alt=\"{1}\" />",
                        src, alt));
                }
                while (nStart > 0);
            }
        }

        List<string> Extract(string start, string end, string xStart, string xEnd)
        {
            int nStart, nEnd;
            string para;
            List<string> values = new List<string>();

            foreach (StringBuilder paragraph in _paragraphs)
            {
                nStart = -1;

                do
                {
                    para = paragraph.ToString();
                    nStart = para.IndexOf(start, nStart + 1);

                    if (!Found(nStart, para))
                        continue;

                    nEnd = para.IndexOf(end, nStart);

                    if (nEnd < 0)
                        continue;

                    values.Add(para.Substring(nStart + start.Length, nEnd - nStart - start.Length));

                    paragraph.Remove(nStart, start.Length + nEnd - nStart);
                    paragraph.Insert(nStart, string.Format("{0}{1}{2}", xStart, values.Count - 1, xEnd));
                }
                while (nStart > 0);
            }

            return values;
        }

        List<string> Extract(string startsWith, char[] endWord, string xStart, string xEnd)
        {
            int nStart;
            string para;
            string[] parts;
            List<string> values = new List<string>();

            foreach (StringBuilder paragraph in _paragraphs)
            {
                nStart = -1;

                do
                {
                    para = paragraph.ToString();
                    nStart = para.IndexOf(startsWith, nStart + 1);

                    if (!Found(nStart, para))
                        continue;

                    parts = para.Substring(nStart).Split(' ');
                    parts[0].TrimEnd(endWord);

                    values.Add(parts[0]);

                    paragraph.Remove(nStart, parts[0].Length);
                    paragraph.Insert(nStart, string.Format("{0}{1}{2}", 
                        xStart, values.Count - 1, xEnd));
                }
                while (nStart > 0);
            }

            return values;
        } 

        void Substitute(string[] filter, string start, string end)
        {
            string[] parts;
            StringBuilder sb;

            foreach (StringBuilder paragraph in _paragraphs)
            {
                parts = paragraph.ToString().Split(filter, StringSplitOptions.None);

                if (parts.Length < 2)
                    continue;

                sb = new StringBuilder(parts[0]);
                bool inside = false;

                for (int i = 1; i < parts.Length; ++i)
                {
                    if (sb.Length >  1 && sb[sb.Length - 1] == '~') // Escaped
                    {
                        sb.Remove(sb.Length - 1, 1);
                        sb.Append(filter[0]);
                    }
                    else
                    {
                        sb.Append(inside ? end : start);
                        inside = !inside;
                    }
                
                    sb.Append(parts[i]);
                }

                if (inside)
                    sb.Append(end);

                paragraph.Remove(0, paragraph.Length);
                paragraph.Append(sb.ToString());
            }
        }

        void Substitute(string[] filter, string start)
        {
            string[] parts;
            StringBuilder sb;

            foreach (StringBuilder paragraph in _paragraphs)
            {
                parts = paragraph.ToString().Split(filter, StringSplitOptions.None);

                if (parts.Length < 2)
                    continue;

                sb = new StringBuilder(parts[0]);

                for (int i = 1; i < parts.Length; ++i)
                {
                    if (sb[sb.Length - 1] == '~') // Escaped
                    {
                        sb.Remove(sb.Length - 1, 1);
                        sb.Append(filter[0]);
                    }
                    else
                        sb.Append(start);

                    sb.Append(parts[i]);
                }

                paragraph.Remove(0, paragraph.Length);
                paragraph.Append(sb.ToString());
            }
        }

        void SubstituteEscapes()
        {
            int nStart;
            string para;
 
            foreach (StringBuilder paragraph in _paragraphs)
            {
                nStart = -1;

                do
                {
                    para = paragraph.ToString();
                    nStart = para.IndexOf("~", nStart + 1);

                    if (nStart < 0)
                        continue;

                    if (nStart == para.Length - 1)
                        continue; // at end nothing to escape
                    if (nStart < para.Length - 2 && para[nStart + 1] == '~')
                        continue; // escaped escape

                    paragraph.Remove(nStart, 1);
                }
                while (nStart > 0);
            }
        }

        void SubstituteTables()
        {
            string line;

            foreach (StringBuilder paragraph in _paragraphs)
            {
                line = paragraph.ToString().Trim();

                if (line.Length < 1 || line[0] != '|')
                    continue;

                line = line.Substring(1, line.EndsWith("|")
                    ? line.Length - 2
                    : line.Length - 1);

                StringBuilder row = new StringBuilder(
                    ++_tableRowCount % 2 == 0
                        ? "<tr>"
                        : "<tr class=\"alt\">");

                foreach (string column in line.Split('|'))
                {
                    if (column.Length > 0 && column[0] == '=')
                    {
                        row.Append("<th>");
                        row.Append(column.Substring(1).Trim());
                        row.Append("</th>");
                    }
                    else
                    {
                        row.Append("<td>");
                        row.Append(column.Trim());
                        row.Append("</td>");
                    }
                }

                row.Append("</tr>");

                paragraph.Remove(0, paragraph.Length);
                paragraph.Insert(0, row.ToString());
            }
        }

        static StringBuilder Header(string key, string line)
        {
            if (!line.StartsWith(key))
                return null;

            int lenKey = key.Length;
            int length = line.EndsWith(key)
                ? line.Length - 2 * lenKey
                : line.Length - lenKey;

            line = length <= 0
                ? "NO HEADER TEXT"
                : line.Substring(key.Length, length).Trim();

            line = ReplaceHtmlEscapes(new StringBuilder(line)).ToString();

            return new StringBuilder(
                string.Format("<h{0}>{1}</h{2}>", lenKey, line, lenKey));
        }
    }
}

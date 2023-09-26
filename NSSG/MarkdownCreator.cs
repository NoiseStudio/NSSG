using Markdig;
using System.Collections.Concurrent;
using System.Text;
using System.Xml;

namespace NSSG;

internal class MarkdownCreator {

    public string Path { get;  }
    public int PathCount { get; }
    public string FullPagesPath { get; }
    public IEnumerable<Fragment> Templates { get; }

    public MarkdownCreator(string path, int pathCount, string fullPagesPath, IEnumerable<Fragment> templates) {
        Path = path;
        PathCount = pathCount;
        FullPagesPath = fullPagesPath;
        Templates = templates;
    }

    public void Create() {
        MarkdownFile[][] markdownFiles = GetMarkdownFiles();

        Parallel.ForEach(markdownFiles, files => {
            StringBuilder builder = new StringBuilder();
            foreach (MarkdownFile markdownFile in files) {
                builder.Append("<ul>");
                foreach (MarkdownFile m in files) {
                    builder.Append("<li");
                    if (markdownFile == m)
                        builder.Append(" class=\"current\"");
                    builder.Append("><a href=\"").Append(m.Url).Append("\">").Append(m.Navigator).Append("</a></li>");
                }
                builder.Append("</ul>");

                markdownFile.Attributes.Add("md_navigator", builder.ToString());
                builder.Clear();

                XmlDocument document = (XmlDocument)markdownFile.Template.Document.Clone();
                document.ReplaceAttributes(markdownFile.Attributes);

                string path = markdownFile.Path[(PathCount + Settings.PagesPath.Length + 1)..];
                path = path[..path.LastIndexOf('.')] + ".html";
                string finalPath = System.IO.Path.Combine(Path, "output", path);
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(finalPath)!);
                File.WriteAllText(finalPath, document.OuterXml);
            }
        });
    }

    private MarkdownFile[][] GetMarkdownFiles() {
        ConcurrentDictionary<string, ConcurrentBag<MarkdownFile>> markdownFiles =
            new ConcurrentDictionary<string, ConcurrentBag<MarkdownFile>>();

        Parallel.ForEach(Directory.EnumerateFiles(FullPagesPath, "*.md", SearchOption.AllDirectories), x => {
            Fragment? template = null;
            foreach (Fragment t in Templates)
            {
                if (x.StartsWith(t.Name) && (template is null || t.Name.Length > template.Name.Length))
                    template = t;
            }

            if (template is null)
                throw new InvalidOperationException("Markdown files not contains template.");

            string text = File.ReadAllText(x);
            string[] lines = text.Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

            Dictionary<string, string> attributes = new Dictionary<string, string>();

            int count = 0;
            foreach (string line in lines)
            {
                count += line.Length;
                if (line == "---")
                {
                    if (count > 3)
                        break;
                    continue;
                }

                int index = line.IndexOf(':');
                string key = line[..index].Trim();
                string value = line[(index + 1)..].Trim();

                attributes.Add(key, value);
            }

            string content = text[(text[3..].IndexOf("---") + 6)..];
            attributes.Add("md_content", Markdown.ToHtml(content));

            string url = x[(PathCount + Settings.PagesPath.Length + 1)..];
            url = Settings.RootUrl + url[..url.LastIndexOf('.')] + Settings.PageExtension;
            url = url.Replace('\\', '/');

            markdownFiles.GetOrAdd("", _ => new ConcurrentBag<MarkdownFile>()).Add(new MarkdownFile(
                x, url, template, attributes
            ));
        });

        return markdownFiles.Values.Select(x => x.OrderBy(x => x.Path).ToArray()).ToArray();
    }

}

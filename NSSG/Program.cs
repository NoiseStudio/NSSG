using NSSG;
using System.Collections.Concurrent;
using System.Xml;

string path = Directory.GetCurrentDirectory();
int pathCount = path.EndsWith('/') || path.EndsWith('\\') ? path.Length : path.Length + 1;

// Load fragments.
const string fragmentsPath = "fragments";
ConcurrentBag<Fragment> fragments = new ConcurrentBag<Fragment>();

string fullFragmentsPath = Path.Combine(path, fragmentsPath);
if (Directory.Exists(fullFragmentsPath)) {
    Parallel.ForEach(Directory.EnumerateFiles(fullFragmentsPath, "*.html", SearchOption.AllDirectories), x => {
        XmlDocument document = new XmlDocument();
        document.Load(x);

        int i = pathCount + fragmentsPath.Length + 1;
        string name = x.Substring(pathCount + fragmentsPath.Length + 1, x.LastIndexOf('.') - i)
            .Replace('\\', '/');
        fragments.Add(new Fragment(name, document));
    });
}

// Compute pages.
string fullPagesPath = Path.Combine(path, Settings.PagesPath);
if (!Directory.Exists(fullPagesPath))
    return;

Directory.CreateDirectory("output");

ConcurrentBag<Fragment> templates = new ConcurrentBag<Fragment>();
Parallel.ForEach(Directory.EnumerateFiles(fullPagesPath, "*.html", SearchOption.AllDirectories), x => {
    XmlDocument document = new XmlDocument();
    document.Load(x);

    foreach (Fragment fragment in fragments) {
        XmlNodeList? nodeList = document.GetElementsByTagName(fragment.Name);
        if (nodeList is null)
            continue;

        for (int i = 0; i < nodeList.Count; i++) {
            XmlNode node = nodeList[i]!;
            fragment.ReplaceNode(node);
        }
    }

    foreach (XmlNode node in document.SelectNodes("//comment()")!)
        node.ParentNode!.RemoveChild(node);

    if (Path.GetFileName(x) == "_template.html") {
        templates.Add(new Fragment(Path.GetDirectoryName(x)!, document));
        return;
    }

    string name = x.Substring(pathCount + Settings.PagesPath.Length + 1);
    string finalPath = Path.Combine(path, "output", name);
    Directory.CreateDirectory(Path.GetDirectoryName(finalPath)!);
    File.WriteAllText(finalPath, document.OuterXml);
});

// Compute markdown files.
new MarkdownCreator(path, pathCount, fullPagesPath, templates).Create();

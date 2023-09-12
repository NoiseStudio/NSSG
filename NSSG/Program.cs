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
const string pagesPath = "pages";

string fullPagesPath = Path.Combine(path, pagesPath);
if (!Directory.Exists(fullPagesPath))
    return;

Directory.CreateDirectory("output");
    
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

    string name = x.Substring(pathCount + pagesPath.Length + 1);
    File.WriteAllText(Path.Combine(path, "output", name), document.OuterXml);
});
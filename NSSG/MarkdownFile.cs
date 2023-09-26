namespace NSSG;

internal class MarkdownFile {

    public string Path { get; }
    public string Url { get; }
    public Fragment Template { get; }
    public Dictionary<string, string> Attributes { get; }
    public string Navigator => Attributes["md_title"];

    public MarkdownFile(string path, string url, Fragment template, Dictionary<string, string> attributes) {
        Path = path;
        Url = url;
        Template = template;
        Attributes = attributes;
    }

}

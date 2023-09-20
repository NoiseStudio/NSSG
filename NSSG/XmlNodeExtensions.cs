using System.Xml;

namespace NSSG;

internal static class XmlNodeExtensions {

    public static XmlNode ReplaceAttributes(this XmlNode node, IReadOnlyDictionary<string, string> attributes) {
        ReplaceAttributesInner(node, attributes);

        foreach (XmlNode n in node.ChildNodes) {
            ReplaceAttributesInner(n, attributes);
            ReplaceAttributes(n, attributes);
        }

        return node;
    }

    private static void ReplaceAttributesInner(this XmlNode node, IReadOnlyDictionary<string, string> attributes)
    {
        if (node.ChildNodes.Count != 0)
            return;

        if (node.NodeType == XmlNodeType.Text && node.ParentNode is not null)
            node.ParentNode!.InnerXml = ReplaceAttributesText(node.InnerText, attributes);

        XmlAttributeCollection? a = node.Attributes;
        if (a is null)
            return;

        foreach (XmlAttribute attribute in a)
            attribute.Value = ReplaceAttributesText(attribute.Value, attributes);
    }

    private static string ReplaceAttributesText(string value, IReadOnlyDictionary<string, string> attributes) {
        while (true) {
            int startIndex = value.IndexOf("${");
            int endIndex = value.IndexOf('}');

            if (startIndex == -1 || endIndex == -1)
                return value;
            if (endIndex < startIndex)
                throw new NotImplementedException();

            startIndex += 2;
            string name = value[startIndex..endIndex];
            value = value.Replace($"${{{name}}}", attributes[name]);
        }
    }

}

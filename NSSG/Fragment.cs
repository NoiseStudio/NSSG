using System.Xml;

namespace NSSG;

internal class Fragment {

    public string Name { get; }
    public XmlDocument Document { get; }

    public Fragment(string name, XmlDocument document) {
        Name = name;
        Document = document;
    }
    public void ReplaceNode(XmlNode nodeToReplace) {
        XmlNode newNode = nodeToReplace.OwnerDocument!.ImportNode(Document.DocumentElement!, true);
        ReplaceContinue(newNode, nodeToReplace);

        XmlAttributeCollection? attributes = nodeToReplace.Attributes;

        nodeToReplace.ParentNode!.ReplaceChild(newNode, nodeToReplace);
        if (attributes is null)
            return;

        ReplaceAttributesInner(newNode, attributes);
        ReplaceAttributes(newNode, attributes);
    }

    private void ReplaceContinue(XmlNode newNode, XmlNode nodeToReplace) {
        foreach (XmlNode node in newNode.ChildNodes) {
            if (node.Name != "Continue") {
                ReplaceContinue(node, nodeToReplace);
                continue;
            }

            XmlNode last = node;
            foreach (XmlNode child in nodeToReplace.ChildNodes) {
                XmlNode n = child.CloneNode(true);
                newNode.InsertAfter(n, last);
                last = n;
            }
            newNode.RemoveChild(node);
            break;
        }
    }

    private void ReplaceAttributes(XmlNode newNode, XmlAttributeCollection attributes) {
        foreach (XmlNode node in newNode.ChildNodes) {
            ReplaceAttributesInner(node, attributes);
            ReplaceAttributes(node, attributes);
        }
    }

    private void ReplaceAttributesInner(XmlNode newNode, XmlAttributeCollection attributes) {
        if (newNode.ChildNodes.Count != 0)
            return;

        if (newNode.InnerText.StartsWith("${") && newNode.InnerText.EndsWith("}")) {
            string name = newNode.InnerText[2..^1];
            if (attributes[name] is not null)
                newNode.InnerText = attributes[name]!.Value;
        }

        XmlAttributeCollection? a = newNode.Attributes;
        if (a is null)
            return;

        foreach (XmlAttribute attribute in a) {
            if (attribute.Value.StartsWith("${") && attribute.Value.EndsWith("}")) {
                string name = attribute.Value[2..^1];
                if (attributes[name] is not null)
                    attribute.Value = attributes[name]!.Value;
            }
        }
    }

}

using System.Xml;

namespace Codecat.Minification;

internal sealed class XmlMinifier : IContentMinifier
{
    public IReadOnlyCollection<string> Languages { get; } = ["xml"];

    public bool TryMinify(string content, out string minified)
    {
        try
        {
            var document = new XmlDocument
            {
                PreserveWhitespace = false
            };
            document.LoadXml(content);

            using var stringWriter = new StringWriter();
            using var xmlWriter = XmlWriter.Create(stringWriter, new XmlWriterSettings
            {
                Indent = false,
                OmitXmlDeclaration = false
            });

            document.Save(xmlWriter);
            xmlWriter.Flush();
            minified = stringWriter.ToString();
            return true;
        }
        catch (XmlException)
        {
            minified = content;
            return false;
        }
    }
}

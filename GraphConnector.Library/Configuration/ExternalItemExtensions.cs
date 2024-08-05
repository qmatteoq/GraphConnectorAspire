using Microsoft.Graph.Models.ExternalConnectors;
using System.ServiceModel.Syndication;

namespace GraphConnector.Library.Configuration
{
    public static class ExternalItemExtensions
    {

        public static ExternalItem ToExternalItem(this SyndicationItem item)
        {
            string slug = string.Empty;
            if (Uri.TryCreate(item.Id, UriKind.Absolute, out Uri uri))
            {
                var segments = uri.Segments;
                int index = Array.IndexOf(segments, "p/") + 1;
                if (index > 0 && index < segments.Length)
                {
                    slug = segments[index].Trim('/');
                }
            }


            var externalItem = new ExternalItem
            {
                Id = slug,
                Properties = new()
                {
                    AdditionalData = new Dictionary<string, object>
                      {
                          { "title", item.Title.Text ?? "" },
                          { "description", item.Title.Text ?? "" },
                          { "url", item.Id }
                          //{ "published", item.PublishDate.Date }
                      }
                },
                Content = new()
                {
                    Value = item.Summary.Text ?? "",
                    Type = ExternalItemContentType.Html
                },
                Acl = new()
                  {
                      new()
                      {
                          Type = AclType.Everyone,
                          Value = "everyone",
                          AccessType = AccessType.Grant
                      }
                  }
            };

            return externalItem;
        }
    }
}

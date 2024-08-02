using System.Reflection;
using System.Text.Json;
using Microsoft.Graph.Models;
using Microsoft.Graph.Models.ExternalConnectors;

namespace GraphConnector.Library.Configuration
{
    public class ConnectionConfiguration : IConnectionConfiguration
    {
        private Dictionary<string, object>? _layout;
        private Dictionary<string, object> Layout
        {
            get
            {
                if (_layout is null)
                {
                    string assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                    string filePath = Path.Combine(assemblyPath, "resultsLayout.json");
                    var adaptiveCard = File.ReadAllText(filePath);
                    _layout = JsonSerializer.Deserialize<Dictionary<string, object>>(adaptiveCard);
                }
                return _layout!;
            }
        }

        public ExternalConnection GetExternalConnection(string connectorId, string connectorName, string connectorDescription)
        {

            return new ExternalConnection
            {
                Id = connectorId,
                Name = connectorName,
                Description = connectorDescription,
                SearchSettings = new()
                {
                    SearchResultTemplates = new()
                    {
                        new()
                        {
                            Id = connectorId,
                            Priority = 1,
                            Layout = new Json
                            {
                                AdditionalData = Layout
                            }
                        }
                    }
                }
            };
        }

        //public static ExternalConnection ExternalConnection => new ExternalConnection
        //{
        //    Id = "learndocs",
        //    Name = "Docs",
        //    Description = "Documentation for Microsoft Graph API which explains what Microsoft Graph is and how to use it.",
        //    //ActivitySettings = new()
        //    //{
        //    //    UrlToItemResolvers = new()
        //    //    {
        //    //        new ItemIdResolver
        //    //        {
        //    //            UrlMatchInfo = new()
        //    //            {
        //    //                BaseUrls = new() { "https://learn.microsoft.com" },
        //    //                UrlPattern = "/[^/]+/graph/auth/(?<slug>[^/]+)",
        //    //            },
        //    //            ItemId = "auth__{slug}",
        //    //            Priority = 1
        //    //        },
        //    //        new ItemIdResolver
        //    //        {
        //    //            UrlMatchInfo = new()
        //    //            {
        //    //                BaseUrls = new() { "https://learn.microsoft.com" },
        //    //                UrlPattern = "/[^/]+/graph/sdks/(?<slug>[^/]+)",
        //    //            },
        //    //            ItemId = "sdks__{slug}",
        //    //            Priority = 2
        //    //        },
        //    //        new ItemIdResolver
        //    //        {
        //    //            UrlMatchInfo = new()
        //    //            {
        //    //                BaseUrls = new() { "https://learn.microsoft.com" },
        //    //                UrlPattern = "/[^/]+/graph/(?<slug>[^/]+)",
        //    //            },
        //    //            ItemId = "{slug}",
        //    //            Priority = 3
        //    //        }
        //    //    }
        //    //},
        //    SearchSettings = new()
        //    {
        //        SearchResultTemplates = new()
        //        {
        //            new()
        //            {
        //                Id = "msgraphdocs",
        //                Priority = 1,
        //                Layout = new Json
        //                {
        //                    AdditionalData = Layout
        //                }
        //            }
        //        }
        //    }
        //};

        public Schema GetSchema()
        {
            return new Schema
            {
                BaseType = "microsoft.graph.externalItem",
                Properties = new()
            {
                new Property
                {
                    Name = "title",
                    Type = PropertyType.String,
                    IsQueryable = true,
                    IsSearchable = true,
                    IsRetrievable = true,
                    Labels = new() { Label.Title }
                },
                new Property
                {
                    Name = "description",
                    Type = PropertyType.String,
                    IsQueryable = true,
                    IsSearchable = true,
                    IsRetrievable = true
                },
                new Property
                {
                    Name = "url",
                    Type = PropertyType.String,
                    IsRetrievable = true,
                    Labels = new() { Label.Url }
                },
                new Property
                {
                    Name = "publishDate",
                    Type = PropertyType.DateTime,
                    IsRetrievable = true,
                    Labels = new() { Label.CreatedDateTime }
                }
            }
            };
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Syndication;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace GraphConnector.Library.Configuration
{
    public class RssParser
    {
        public IEnumerable<SyndicationItem> ParseRss(string rssUrl)
        {
            using (XmlReader reader = XmlReader.Create(rssUrl))
            {
                SyndicationFeed feed = SyndicationFeed.Load(reader);
                return feed.Items;
            }
        }
    }
}

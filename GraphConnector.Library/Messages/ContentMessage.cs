namespace GraphConnector.Library.Messages
{
    public enum CrawlType
    {
        Full,
        Incremental,
        RemoveDeleted
    }

    public enum ItemAction
    {
        Update,
        Delete
    }

    public enum ContentAction
    {
        Create,
        Delete
    }

    public class ContentMessage
    {
        public ContentAction? Action { get; set; }
        public CrawlType? CrawlType { get; set; }
        public ItemAction? ItemAction { get; set; }
        public string? Url { get; set;}

        public string? ConnectorId { get; set; }
    }
}

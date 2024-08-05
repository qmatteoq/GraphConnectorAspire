using GraphConnector.Library.Enums;

namespace GraphConnector.Library.Responses
{
    public class OperationStatusResponse
    {
        public OperationStatus Status { get; set; }
        public DateTimeOffset LastStatusDate { get; set; }
    }
}

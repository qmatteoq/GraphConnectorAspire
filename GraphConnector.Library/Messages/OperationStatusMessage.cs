using GraphConnector.Library.Enums;

namespace GraphConnector.Library.Messages
{
    public class OperationStatusMessage
    {
        public OperationStatus Status { get; set; }
        public DateTime LastStatusDate { get; set; }
    }
}

namespace PartitionUtility
{
    public class LogMessage
    {
        public LogMessage(string status, string time, string message)
        {
            Status = status;
            Time = time;
            Message = message;
        }

        public string Status { get; set; }
        public string Time { get; set; }
        public string Message { get; set; }
    }
}

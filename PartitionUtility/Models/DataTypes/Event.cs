namespace PartitionUtility
{
    public class Event
    {
        public string Date { get; set; }
        public string TimeZ { get; set; }
        public string Interval { get; set; }
        public string Name { get; set; }
        public string User { get; set; }
        public string QueryText { get; set; }

        public static bool operator == (Event event1, Event event2)
        {
            if (ReferenceEquals(event1, null) || ReferenceEquals(event2, null)) return ReferenceEquals(event1, event2);
            return event1.Date == event2.Date && event1.TimeZ == event2.TimeZ && event1.Interval == event2.Interval && event1.Name == event2.Name && event1.User == event2.User && event1.QueryText == event2.QueryText;
        }

        public static bool operator != (Event event1, Event event2)
        {
            return !(event1 == event2);
        }
    }
}

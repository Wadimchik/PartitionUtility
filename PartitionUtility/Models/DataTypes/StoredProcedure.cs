namespace PartitionUtility
{
    public class StoredProcedure
    {
        public string Date { get; set; }
        public string Name { get; set; }
        public string User { get; set; }
        public string QueryText { get; set; }

        public static bool operator == (StoredProcedure procedure1, StoredProcedure procedure2)
        {
            if (ReferenceEquals(procedure1, null) || ReferenceEquals(procedure2, null)) return ReferenceEquals(procedure1, procedure2);
            return procedure1.Date == procedure2.Date && procedure1.Name == procedure2.Name && procedure1.User == procedure2.User && procedure1.QueryText == procedure2.QueryText;
        }

        public static bool operator != (StoredProcedure procedure1, StoredProcedure procedure2)
        {
            return !(procedure1 == procedure2);
        }
    }
}

namespace PartitionUtility
{
    public static class IntervalMeasureToStringConverter
    {
        public static string Convert(int measure)
        {
            switch(measure)
            {
                case 0:
                    return "MINUTE";

                case 1:
                    return "HOUR";

                case 2:
                    return "DAY";

                case 3:
                    return "MONTH";
            }
            return "";
        }
    }
}

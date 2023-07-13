namespace PartitionUtility
{
    public static class MeasureComparer
    {
        public static decimal Convert(int index)
        {
            switch (index)
            {
                case 0:
                    return 3600;

                case 1:
                    return 60;

                case 2:
                    return 1;

                case 3:
                    return 1 / 24m;
            }
            return 0;
        }

        public static decimal ConvertToHours(int index)
        {
            switch(index)
            {
                case 0:
                    return 24;

                case 1:
                    return 720;

                case 2:
                    return 8760;
            }
            return 0;
        }

        public static decimal ScaleValue(decimal value, int firstMeasure, int secondMeasure)
        {
            switch (firstMeasure)
            {
                case 0:
                    if (secondMeasure == 1) return value *= 30;
                    if (secondMeasure == 2) return value *= 365;
                    break;

                case 1:
                    if (secondMeasure == 0) return value /= 30;
                    if (secondMeasure == 2) return value *= 12;
                    break;

                case 2:
                    if (secondMeasure == 0) return value /= 365;
                    if (secondMeasure == 1) return value /= 12;
                    break;
            }
            return value;
        }
    }
}

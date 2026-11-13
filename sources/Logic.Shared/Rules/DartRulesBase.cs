namespace Logic.Shared.Rules
{
    public abstract class DartRulesBase
    {
        public bool IsValidSegment(int segment)
        {
            return segment == 0 ||
                   segment is >= 1 and <= 20 ||
                   segment == 25;
        }

        public bool IsValidMultiplier(int segment, int multiplier)
        {
            return segment switch
            {
                0 => multiplier == 0,
                >= 1 and <= 20 => multiplier is >= 1 and <= 3,
                25 => multiplier is 1 or 2,
                _ => false
            };
        }

        public int CalculateScore(int segment, int multiplier)
        {
            if (!IsValidMultiplier(segment, multiplier) ||
                !IsValidSegment(segment))
            {
                throw new ArgumentException("Invalid dart throw.");
            }

            return segment * multiplier;
        }

        public bool IsDouble(int segment, int multiplier)
        {
            return IsValidMultiplier(segment, multiplier) &&
                   multiplier == 2;
        }

        public bool IsTriple(int segment, int multiplier)
        {
            return IsValidMultiplier(segment, multiplier) &&
                   multiplier == 3;
        }

        public bool IsOuterBull(int segment, int multiplier)
        {
            return IsValidMultiplier(segment, multiplier) &&
                   segment == 25 &&
                   multiplier == 1;
        }

        public bool IsInnerBull(int segment, int multiplier)
        {
            return IsValidMultiplier(segment, multiplier) &&
                   segment == 25 &&
                   multiplier == 2;
        }
    }
}

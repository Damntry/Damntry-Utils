namespace Damntry.Utils.Numerics {

    /// <summary>
    /// Increases or decreases a value within a given range, and once a range limit 
    /// is exceeded, the value is rolled over/under into the other range limit.
    /// </summary>
    /// <param name="minValueInclusive">The minimum possible accepted value.</param>
    /// <param name="maxValueInclusive">The maximum possible accepted value.</param>
    /// <param name="step">
    /// The current value will be increased (or decreased if negative) by this value.
    /// The value will roll over and respect ranges in either direction.
    /// </param>
    public class RollingIntegerRange(int minValueInclusive, int maxValueInclusive, int step = 1) {

        private int currentValue;

        public int GetNextValue() {
            currentValue += step;

            if (currentValue > maxValueInclusive) {
                currentValue = minValueInclusive;
            } else if (currentValue < minValueInclusive) {
                currentValue = maxValueInclusive;
            }

            return currentValue;
        }

    }
}

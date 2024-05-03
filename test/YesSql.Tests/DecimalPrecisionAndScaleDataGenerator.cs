using Xunit;

namespace YesSql.Tests
{

    internal sealed class DecimalPrecisionAndScaleDataGenerator : TheoryData<byte?, byte?>
    {
        public DecimalPrecisionAndScaleDataGenerator()
        {
            Add(null, null);
            Add(1, null);
            Add(null, 2);
            Add(1, 2);
        }
    }
}

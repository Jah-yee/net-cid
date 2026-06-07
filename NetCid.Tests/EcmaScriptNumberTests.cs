using System.Globalization;

namespace NetCid.Tests;

public sealed class EcmaScriptNumberTests
{
    [Theory]
    [InlineData(0.0, "0")]
    [InlineData(1.0, "1")]
    [InlineData(-1.0, "-1")]
    [InlineData(1.5, "1.5")]
    [InlineData(0.1, "0.1")]
    [InlineData(100.0, "100")]
    [InlineData(1e20, "100000000000000000000")]
    [InlineData(1e21, "1e+21")]
    [InlineData(1e-6, "0.000001")]
    [InlineData(1e-7, "1e-7")]
    [InlineData(5e-324, "5e-324")]
    [InlineData(1.7976931348623157e308, "1.7976931348623157e+308")]
    [InlineData(9007199254740992.0, "9007199254740992")]
    public void Vector_Table_From_Issue_13(double value, string expected)
    {
        Assert.Equal(expected, EcmaScriptNumber.ToCanonicalString(value));
    }

    [Fact]
    public void Negative_Zero_Collapses_To_Zero()
    {
        // -0.0 and 0.0 compare equal under IEEE 754, so xUnit's [InlineData] sees them
        // as duplicates and won't let us include both in the table above.
        Assert.Equal("0", EcmaScriptNumber.ToCanonicalString(-0.0));
    }

    [Fact]
    public void Threshold_1e21_Goes_Exponential()
    {
        // 1e20 just below the exponential threshold; 1e21 just at/over.
        Assert.Equal("100000000000000000000", EcmaScriptNumber.ToCanonicalString(1e20));
        Assert.Equal("1e+21", EcmaScriptNumber.ToCanonicalString(1e21));
    }

    [Fact]
    public void Threshold_1e_minus_6_Switches_To_Decimal()
    {
        // 1e-7 must use exponential; 1e-6 must use decimal form.
        Assert.Equal("1e-7", EcmaScriptNumber.ToCanonicalString(1e-7));
        Assert.Equal("0.000001", EcmaScriptNumber.ToCanonicalString(1e-6));
    }

    [Fact]
    public void Double_Epsilon_Canonicalises_As_Smallest_Subnormal()
    {
        Assert.Equal("5e-324", EcmaScriptNumber.ToCanonicalString(double.Epsilon));
    }

    [Fact]
    public void Double_MaxValue_Canonicalises_As_Largest_Finite()
    {
        Assert.Equal("1.7976931348623157e+308", EcmaScriptNumber.ToCanonicalString(double.MaxValue));
    }

    [Fact]
    public void Double_MinValue_Canonicalises_As_Negative_Largest_Finite()
    {
        Assert.Equal("-1.7976931348623157e+308", EcmaScriptNumber.ToCanonicalString(double.MinValue));
    }

    [Fact]
    public void NaN_Throws_Defensively()
    {
        Assert.Throws<JcsFormatException>(() => EcmaScriptNumber.ToCanonicalString(double.NaN));
    }

    [Fact]
    public void Positive_Infinity_Throws_Defensively()
    {
        Assert.Throws<JcsFormatException>(() => EcmaScriptNumber.ToCanonicalString(double.PositiveInfinity));
    }

    [Fact]
    public void Negative_Infinity_Throws_Defensively()
    {
        Assert.Throws<JcsFormatException>(() => EcmaScriptNumber.ToCanonicalString(double.NegativeInfinity));
    }

    [Fact]
    public void Deterministic_Fuzz_Roundtrips_Every_Bit_Pattern()
    {
        // 100k random ulong bit-patterns reinterpreted as doubles. Skip NaN/±∞/0. For
        // every other value the canonical string must parse back to the same 64-bit
        // pattern — proving EcmaScriptNumber preserves IEEE-754 identity even without
        // the upstream RFC 8785 conformance file.
        const int iterations = 100_000;
        var rng = new Random(Seed: 20260606);
        var bytes = new byte[8];
        var mismatches = new List<string>();

        for (var i = 0; i < iterations; i++)
        {
            rng.NextBytes(bytes);
            var bits = BitConverter.ToUInt64(bytes, 0);
            var value = BitConverter.UInt64BitsToDouble(bits);

            if (double.IsNaN(value) || double.IsInfinity(value) || value == 0.0)
            {
                continue;
            }

            var canonical = EcmaScriptNumber.ToCanonicalString(value);
            var roundTripped = double.Parse(canonical, CultureInfo.InvariantCulture);
            var roundTrippedBits = BitConverter.DoubleToUInt64Bits(roundTripped);

            if (roundTrippedBits != bits && mismatches.Count < 20)
            {
                mismatches.Add(
                    $"bits=0x{bits:X16} value={value:R} canonical='{canonical}' " +
                    $"roundTrippedBits=0x{roundTrippedBits:X16}");
            }
        }

        Assert.True(
            mismatches.Count == 0,
            "Round-trip mismatches:" + Environment.NewLine + string.Join(Environment.NewLine, mismatches));
    }
}

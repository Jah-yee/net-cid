using System.IO.Compression;

namespace NetCid.Tests;

/// <summary>
/// Runs the RFC 8785 number conformance test set
/// (<see href="https://github.com/cyberphone/json-canonicalization/tree/master/testdata/numbers"/>)
/// against <see cref="EcmaScriptNumber.ToCanonicalString(double)"/>.
/// </summary>
/// <remarks>
/// Reads the file path from the <c>NETCID_CONFORMANCE_FILE</c> environment variable
/// (the .github/workflows/jcs-conformance.yml job sets it to a cached, SHA-256-pinned
/// copy of <c>es6testfile100m.txt.gz</c>). When the variable is unset (local
/// developer runs, the standard CI job), the test skips with a clear message rather
/// than failing.
/// </remarks>
public sealed class EcmaScriptNumberConformanceTests
{
    private const string EnvVar = "NETCID_CONFORMANCE_FILE";
    private const int MaxReportedMismatches = 20;

    [Fact]
    public void Conformance_Set_Matches_EcmaScriptNumber_Output()
    {
        var path = Environment.GetEnvironmentVariable(EnvVar);
        if (string.IsNullOrEmpty(path))
        {
            Assert.Skip(
                $"Set {EnvVar} to the path of cyberphone/json-canonicalization's " +
                "es6testfile100m.txt.gz release asset to run the conformance suite. " +
                "The jcs-conformance GitHub Actions workflow does this automatically.");
            return;
        }

        Assert.True(
            File.Exists(path),
            $"{EnvVar}={path} but the file does not exist.");

        using var fileStream = File.OpenRead(path);
        using var gzip = new GZipStream(fileStream, CompressionMode.Decompress);
        using var reader = new StreamReader(gzip);

        var mismatches = new List<string>();
        var processed = 0L;
        string? line;

        while ((line = reader.ReadLine()) is not null)
        {
            processed++;

            var commaIndex = line.IndexOf(',');
            if (commaIndex < 0)
            {
                continue; // Skip malformed / blank lines defensively.
            }

            var hex = line[..commaIndex];
            var expected = line[(commaIndex + 1)..];

            var bits = Convert.ToUInt64(hex, 16);
            var value = BitConverter.UInt64BitsToDouble(bits);

            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                continue; // ECMAScript can't represent these; conformance file shouldn't either.
            }

            var actual = EcmaScriptNumber.ToCanonicalString(value);

            if (!string.Equals(actual, expected, StringComparison.Ordinal)
                && mismatches.Count < MaxReportedMismatches)
            {
                mismatches.Add(
                    $"line {processed:N0}: hex={hex} expected='{expected}' actual='{actual}'");
            }
        }

        Assert.True(
            mismatches.Count == 0,
            $"Processed {processed:N0} rows; first {mismatches.Count} mismatch(es):" +
            Environment.NewLine + string.Join(Environment.NewLine, mismatches));
    }
}

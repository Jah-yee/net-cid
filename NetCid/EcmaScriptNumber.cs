using System.Globalization;

namespace NetCid;

/// <summary>
/// ECMAScript <c>Number.prototype.toString()</c> (ECMA-262 §6.1.6.1.20) for IEEE-754 doubles.
/// RFC 8785 §3.2.2.3 requires this exact textual form for JSON number canonicalization.
/// </summary>
internal static class EcmaScriptNumber
{
    /// <summary>
    /// Returns the canonical ECMA-262 §6.1.6.1.20 string for <paramref name="value"/>.
    /// </summary>
    /// <exception cref="JcsFormatException"><paramref name="value"/> is NaN or ±∞.</exception>
    public static string ToCanonicalString(double value)
    {
        if (double.IsNaN(value))
        {
            throw new JcsFormatException("JCS cannot represent NaN (RFC 8785 §3.2.2.3).");
        }
        if (double.IsPositiveInfinity(value))
        {
            throw new JcsFormatException("JCS cannot represent +infinity (RFC 8785 §3.2.2.3).");
        }
        if (double.IsNegativeInfinity(value))
        {
            throw new JcsFormatException("JCS cannot represent -infinity (RFC 8785 §3.2.2.3).");
        }

        // ECMA-262 step 1: +0 and -0 both produce "0". IEEE 754 guarantees -0.0 == 0.0.
        if (value == 0.0)
        {
            return "0";
        }

        var negative = value < 0.0;
        var abs = negative ? -value : value;

        // .NET 5+ documents the parameterless ToString as the shortest round-trippable
        // decimal. We treat its digit string as ECMAScript's canonical `s` and verify
        // against the RFC 8785 conformance vector.
        var raw = abs.ToString(CultureInfo.InvariantCulture);

        Decompose(raw, out var digits, out var n);

        var body = Format(digits, n);
        return negative ? "-" + body : body;
    }

    private static void Decompose(string raw, out string digits, out int n)
    {
        // .NET emits uppercase 'E' and a zero-padded two-digit exponent ("E+20", "E-07").
        // We never echo that — int.Parse drops the leading zero, and we re-emit the
        // exponent ourselves in lowercase with no padding.
        var eIndex = raw.IndexOfAny(['E', 'e']);
        string mantissa;
        int ePart;
        if (eIndex >= 0)
        {
            mantissa = raw[..eIndex];
            ePart = int.Parse(raw.AsSpan(eIndex + 1), NumberStyles.Integer, CultureInfo.InvariantCulture);
        }
        else
        {
            mantissa = raw;
            ePart = 0;
        }

        var dotIndex = mantissa.IndexOf('.');
        string intPart;
        string fracPart;
        if (dotIndex >= 0)
        {
            intPart = mantissa[..dotIndex];
            fracPart = mantissa[(dotIndex + 1)..];
        }
        else
        {
            intPart = mantissa;
            fracPart = string.Empty;
        }

        var rawDigits = intPart + fracPart;

        var lead = 0;
        while (lead < rawDigits.Length - 1 && rawDigits[lead] == '0')
        {
            lead++;
        }

        var lastNonZero = rawDigits.Length - 1;
        while (lastNonZero > lead && rawDigits[lastNonZero] == '0')
        {
            lastNonZero--;
        }
        var trail = rawDigits.Length - 1 - lastNonZero;

        digits = rawDigits.Substring(lead, lastNonZero - lead + 1);
        n = digits.Length + ePart - fracPart.Length + trail;
    }

    private static string Format(string digits, int n)
    {
        var k = digits.Length;

        // ECMA-262 §6.1.6.1.20 step 5.
        if (k <= n && n <= 21)
        {
            return digits + new string('0', n - k);
        }
        // Step 6.
        if (0 < n && n <= 21)
        {
            return digits[..n] + "." + digits[n..];
        }
        // Step 7.
        if (-6 < n && n <= 0)
        {
            return "0." + new string('0', -n) + digits;
        }
        // Step 8: exponential. Lowercase "e", explicit sign, no zero padding.
        var exp = n - 1;
        var expStr = exp >= 0
            ? "e+" + exp.ToString(CultureInfo.InvariantCulture)
            : "e" + exp.ToString(CultureInfo.InvariantCulture);
        return k == 1
            ? digits + expStr
            : digits[..1] + "." + digits[1..] + expStr;
    }
}

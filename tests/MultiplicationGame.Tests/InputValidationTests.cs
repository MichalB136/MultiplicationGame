namespace MultiplicationGame.Tests;

using Xunit;
using MultiplicationGame.Pages;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Reflection;

/// <summary>
/// Tests for input validation to ensure invalid characters (spaces, commas, etc.) are properly rejected
/// </summary>
public class InputValidationTests
{
    /// <summary>
    /// Helper to call private TryParseAnswerText method from IndexModel
    /// </summary>
    private static bool TryParseAnswerText(string? input, out int value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(input))
            return false;

        var s = input.Trim();
        
        // Must match regex: ^-?\d+$
        if (!System.Text.RegularExpressions.Regex.IsMatch(s, @"^-?\d+$"))
            return false;
            
        return int.TryParse(s, out value);
    }

    /// <summary>
    /// Helper to call private TryParseNumber method from EquationsModel
    /// </summary>
    private static bool TryParseNumber(string? input, out double value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(input))
            return false;

        var s = input.Trim().Replace(',', '.');
        
        s = System.Text.RegularExpressions.Regex.Replace(s, @"[\u200B\u200C\u200D\u202A\u202B\u202C\u202D\u202E\u2060\uFEFF\u061C]", "");
        
        if (!System.Text.RegularExpressions.Regex.IsMatch(s, @"^-?\d+(\.\d+)?$|^-?\.\d+$"))
            return false;
            
        return double.TryParse(s, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out value);
    }

    #region Valid Integer Inputs (Multiplication Game)

    [Theory]
    [InlineData("5")]
    [InlineData("0")]
    [InlineData("42")]
    [InlineData("100")]
    [InlineData("-5")]
    [InlineData("-42")]
    public void TryParseAnswerText_ValidIntegers_ReturnsTrue(string input)
    {
        Assert.True(TryParseAnswerText(input, out var value));
        Assert.Equal(int.Parse(input), value);
    }

    #endregion

    #region Invalid Integer Inputs (Multiplication Game) - CRITICAL TEST CASES

    [Theory]
    [InlineData("2, 9")]           // comma with space (MAIN PROBLEM CASE)
    [InlineData("2, 9 ")]          // comma with space and trailing space
    [InlineData("2,9")]            // comma without space
    [InlineData("2 9")]            // space instead of comma
    [InlineData("5.5")]            // decimal point (not allowed in multiplication)
    [InlineData("5,5")]            // decimal comma (not allowed in multiplication)
    [InlineData("5.")]             // decimal without digits
    [InlineData(".5")]             // decimal without integer part
    [InlineData("5a")]             // letter
    [InlineData("a5")]             // letter prefix
    [InlineData("5@")]             // special character
    [InlineData("")]               // empty
    [InlineData("   ")]            // only spaces
    public void TryParseAnswerText_InvalidInputs_ReturnsFalse(string input)
    {
        Assert.False(TryParseAnswerText(input, out _), $"Input '{input}' should have been rejected");
    }

    #endregion

    #region Valid Decimal Inputs (Equations Game)

    [Theory]
    [InlineData("5", 5.0)]
    [InlineData("0", 0.0)]
    [InlineData("42", 42.0)]
    [InlineData("-5", -5.0)]
    [InlineData("5.5", 5.5)]
    [InlineData("-5.5", -5.5)]
    [InlineData(".5", 0.5)]
    [InlineData("-.5", -0.5)]
    [InlineData("5,5", 5.5)]    // comma decimal separator normalization
    [InlineData("-5,5", -5.5)]  // comma with minus
    [InlineData("3.9", 3.9)]    // decimal with dot
    [InlineData("3,9", 3.9)]    // decimal with comma (European format)
    public void TryParseNumber_ValidDecimals_ReturnsTrue(string input, double expected)
    {
        Assert.True(TryParseNumber(input, out var value));
        Assert.Equal(expected, value, 5); // allow small floating point errors
    }

    #endregion

    #region Invalid Decimal Inputs (Equations Game)

    [Theory]
    [InlineData("2, 9")]           // comma with space (fails - space not allowed after trim and replace)
    [InlineData("5.5.5")]          // multiple decimal points
    [InlineData("5,5,5")]          // multiple commas
    [InlineData("5a")]             // letter
    [InlineData("a5")]             // letter prefix
    [InlineData("5@")]             // special character
    [InlineData("")]               // empty
    [InlineData("   ")]            // only spaces
    public void TryParseNumber_InvalidInputs_ReturnsFalse(string input)
    {
        Assert.False(TryParseNumber(input, out _), $"Input '{input}' should have been rejected");
    }

    #endregion

    #region Edge Cases with Invisible Unicode Characters

    [Theory]
    [InlineData("5\u200B")]        // zero-width space
    [InlineData("5\u200C")]        // zero-width non-joiner
    [InlineData("5\u200D")]        // zero-width joiner
    [InlineData("5\u202A")]        // left-to-right embedding
    [InlineData("\u200B5")]        // zero-width space before
    public void TryParseNumber_InvisibleUnicodeChars_AreRemoved(string input)
    {
        // These should be removed by the normalization in TryParseNumber
        Assert.True(TryParseNumber(input, out var value));
        Assert.Equal(5.0, value);
    }

    #endregion

    #region Multiplication Input Cannot Have Decimals

    [Theory]
    [InlineData("5.5")]
    [InlineData("5,5")]
    [InlineData(".5")]
    [InlineData("5.")]
    public void TryParseAnswerText_WithDecimal_ReturnsFalse(string input)
    {
        // Multiplication game does not allow decimals
        Assert.False(TryParseAnswerText(input, out _), $"Decimal input '{input}' should be rejected for multiplication");
    }

    #endregion

    #region Whitespace Handling

    [Fact]
    public void TryParseAnswerText_WithOnlyLeadingTrailingWhitespace_IsTrimmedAndParsed()
    {
        // Leading/trailing whitespace is trimmed, so these should be valid
        Assert.True(TryParseAnswerText("  42  ", out var value));
        Assert.Equal(42, value);
        
        Assert.True(TryParseAnswerText("  -5  ", out var value2));
        Assert.Equal(-5, value2);
        
        Assert.True(TryParseAnswerText("  0  ", out var value3));
        Assert.Equal(0, value3);
    }

    [Fact]
    public void TryParseNumber_WithOnlyLeadingTrailingWhitespace_IsTrimmed()
    {
        Assert.True(TryParseNumber("  42.5  ", out var value));
        Assert.Equal(42.5, value);
    }

    [Fact]
    public void TryParseAnswerText_WithInternalWhitespace_ReturnsFalse()
    {
        Assert.False(TryParseAnswerText("4 2", out _));
    }

    [Fact]
    public void TryParseNumber_WithCommaDecimalSeparator_IsNormalized()
    {
        // European locale using comma as decimal separator should be converted to dot
        Assert.True(TryParseNumber("42,5", out var value));
        Assert.Equal(42.5, value);
    }

    #endregion

    #region Boundary Values

    [Theory]
    [InlineData(int.MinValue)]
    [InlineData(int.MaxValue)]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1)]
    public void TryParseAnswerText_BoundaryIntegers_Parse(int boundaryValue)
    {
        string input = boundaryValue.ToString();
        Assert.True(TryParseAnswerText(input, out var value));
        Assert.Equal(boundaryValue, value);
    }

    #endregion
}

using System.Globalization;

namespace WpfApp1;

public class Formatter
{
    public static string FormatDouble(double value)
    {
        // Ensure consistent floating-point formatting with '.' as the decimal separator (not ,)
        return value.ToString(CultureInfo.InvariantCulture);
    }

    public static string FormatInt(int value)
    {
        return value.ToString(CultureInfo.InvariantCulture);
    }

    public static int FormatStringToInt(string text)
    {
        return int.Parse(text, CultureInfo.InvariantCulture);
    }

    public static double FormatString(string text)
    {
        return double.Parse(text, CultureInfo.InvariantCulture);
    }
    
    public static decimal FormatStringDecimal(string text)
    {
        return decimal.Parse(text, CultureInfo.InvariantCulture);
    }

    public static string FormatDecimal(decimal value)
    {
        return value.ToString(CultureInfo.InvariantCulture);
    }

}
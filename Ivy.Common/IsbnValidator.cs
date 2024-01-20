namespace Ivy.Common;

public static class IsbnValidator
{
    public static bool IsValidIsbn(string isbn)
    {
        if (isbn.Length == 10)
        {
            return IsValidIsbn10(isbn);
        }

        if (isbn.Length == 13)
        {
            return IsValidIsbn13(isbn);
        }
        
        return false;
    }

    private static bool IsValidIsbn10(string isbn)
    {
        if (isbn.Any(ch => !char.IsDigit(ch) && ch != 'X' && ch != 'x')) 
            return false;

        var sum = 0;
        for (var i = 0; i < 9; i++)
        {
            sum += (isbn[i] - '0') * (i + 1);
        }

        var lastChar = isbn[9];
        if (lastChar == 'X' || lastChar == 'x')
        {
            sum += 10 * 10;
        }
        else if (char.IsDigit(lastChar))
        {
            sum += (lastChar - '0') * 10;
        }
        else
        {
            return false;
        }

        return sum % 11 == 0;
    }

    private static bool IsValidIsbn13(string isbn)
    {
        if (!isbn.All(char.IsDigit)) return false;

        var sum = 0;
        for (var i = 0; i < 12; i++)
        {
            sum += (isbn[i] - '0') * (i % 2 == 0 ? 1 : 3);
        }

        var checksum = (10 - sum % 10) % 10;
        return checksum == isbn[12] - '0';
    }
}

namespace Taiken.FakePos;

internal static class CheckIdFormat
{
    public static string Format(int id) => $"CHK-{id}";

    public static bool TryParse(string checkId, out int id)
    {
        if (checkId.StartsWith("CHK-", StringComparison.Ordinal) && int.TryParse(checkId.AsSpan(4), out id))
        {
            return true;
        }

        id = 0;
        return false;
    }
}

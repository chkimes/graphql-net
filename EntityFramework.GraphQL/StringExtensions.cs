namespace GraphQL.Net
{
    public static class StringExtensions
    {
        public static string ToCamelCase(this string val)
        {
            if (string.IsNullOrEmpty(val))
                return "";
            return char.ToLower(val[0]) + val.Substring(1);
        }
    }
}

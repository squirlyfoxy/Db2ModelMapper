namespace Db2ModelMapper.Core
{
    public static class QueryUtility
    {
        public static string AbjustValue(string value)
        {
            return value
                .Replace("'", "''")
                .Replace(@"\", "\\");
        }
    }
}

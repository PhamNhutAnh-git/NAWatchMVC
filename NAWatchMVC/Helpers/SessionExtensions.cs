using Newtonsoft.Json;
namespace NAWatchMVC.Helpers
{
    public static class SessionExtensions
    {
        public static void Set<T>(this ISession session, string key, T value)
        {
            session.SetString(key, JsonConvert.SerializeObject(value));
        }

        public static T Get<T>(this ISession session, string key)
        {
            var value = session.GetString(key);
            return value == null ? default : JsonConvert.DeserializeObject<T>(value);
        }
        public static void SetDouble(this ISession session, string key, double value)
        {
            session.SetString(key, value.ToString());
        }

        public static double? GetDouble(this ISession session, string key)
        {
            var data = session.GetString(key);
            if (double.TryParse(data, out double result)) return result;
            return null;
        }
    }
}

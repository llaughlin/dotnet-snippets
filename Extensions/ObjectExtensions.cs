using System;
using Newtonsoft.Json;

namespace Extensions
{
    public static class ObjectExtensions
    {
        public static string ToJson(this object source)
        {
            return JsonConvert.SerializeObject(source);
        }

        public static T With<T>(this T source, Action<T> accessor)
        {
            accessor(source);
            return source;
        }
    }
}
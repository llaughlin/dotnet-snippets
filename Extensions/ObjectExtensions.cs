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

        public static T With<T>(this T source, Action<T> accessor) where T : class
        {
            accessor(source);
            return source;
        }
        public static T With<T>(this ref T source, Action<T> accessor) where T : struct
        {
            accessor(source);
            return source;
        }
    }
}
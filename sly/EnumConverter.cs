using System;

namespace sly
{
    public class EnumConverter
    {
        public static T ConvertIntToEnum<T>(int intValue)
        {
            var genericType = typeof(T);
            if (genericType.IsEnum)
                foreach (T value in Enum.GetValues(genericType))
                {
                    var test = Enum.Parse(typeof(T), value.ToString()) as Enum;
                    var val = Convert.ToInt32(test);
                    if (val == intValue)
                    {
                        return value;
                    }
                }

            return default(T);
        }
    }
}
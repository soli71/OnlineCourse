using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace OnlineCourse.Extensions;

public static class EnumHelper<T> where T : Enum
{
    public static string GetDisplayValue(string value)
    {
        var field = typeof(T).GetField(value);
        var attribute = field.GetCustomAttribute<DisplayAttribute>();
        return attribute == null ? value : attribute.GetName();
    }

}

public static class EnumHelper
{

    public static string GetDisplayValue(this Enum value)
    {
        var field = value.GetType().GetField(value.ToString());
        var attribute = field.GetCustomAttribute<DisplayAttribute>();
        return attribute == null ? value.ToString() : attribute.GetName();
    }
}

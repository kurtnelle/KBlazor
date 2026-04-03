using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Drawing;
using System.Linq;
using System.Reflection;

namespace KBlazor.Models
{
    public static class ExtensionMethods
    {
        public static List<List<T>> SplitInto<T>(this List<T> items, int size)
        {
            List<List<T>> list = new List<List<T>>();
            for (int i = 0; i < items.Count; i += size)
                list.Add(items.GetRange(i, Math.Min(size, items.Count - i)));
            return list;
        }

        public static string DisplayNameOrDefault(this PropertyInfo source)
        {
            var displayAttribute = (DisplayAttribute)source.GetCustomAttributes(typeof(DisplayAttribute), false).FirstOrDefault();
            if (displayAttribute != null)
            {
                return displayAttribute.Name;
            }
            else
            {
                return source.Name;
            }
        }

        public static float GetTextSize(this string source, string fontName, float fontSize)
        {
            Font font = new Font(fontName, fontSize);
            return GetTextSize(source, font);
        }

        public static float GetTextSize(this string source, Font font)
        {
            Image fakeImage = new Bitmap(1, 1);
            Graphics graphics = Graphics.FromImage(fakeImage);
            SizeF size = graphics.MeasureString(source, font);
            return size.Width;
        }

        public static bool Contains(this string s, int value)
        {
            return s.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                 .Select(s => int.Parse(s))
                 .Where(w => w == value)
                 .Any();
        }

        public static string GetDescription(this Enum value)
        {
            Type type = value.GetType();
            string name = Enum.GetName(type, value);
            if (name != null)
            {
                FieldInfo field = type.GetField(name);
                if (field != null)
                {
                    DescriptionAttribute attr =
                           Attribute.GetCustomAttribute(field,
                             typeof(DescriptionAttribute)) as DescriptionAttribute;
                    if (attr != null)
                    {
                        return attr.Description;
                    }
                    else
                    {
                        return value.ToString();
                    }
                }
            }
            return null;
        }

        #region Relative Dates
        public static DateTime EndOfDay(this DateTime dt)
        {
            return dt.Date.AddMinutes(1439);
        }
        public static DateTime StartOfWeek(this DateTime dt)
        {
            return dt.Date.AddDays(-1 * (int)dt.DayOfWeek);
        }

        public static DateTime EndOfWeek(this DateTime dt)
        {
            return dt.Date.AddDays(6 - (int)dt.DayOfWeek).EndOfDay();
        }

        public static DateTime StartOfMonth(this DateTime dt)
        {
            return new DateTime(dt.Year, dt.Month, 1);
        }

        public static DateTime EndOfMonth(this DateTime dt)
        {
            return dt.StartOfMonth().AddMonths(1).AddDays(-1).EndOfDay();
        }

        public static DateTime StartOfYear(this DateTime dt)
        {
            return new DateTime(dt.Year, 1, 1);
        }

        public static DateTime EndOfYear(this DateTime dt)
        {
            return new DateTime(dt.Year, 12, 31);
        }

        public static DateTime Yesterday(this DateTime dt)
        {
            return dt.Date.AddDays(-1);
        }

        public static DateTime LastWeek(this DateTime dt)
        {
            return dt.Date.AddDays(-7).StartOfWeek();
        }
        public static DateTime LastMonth(this DateTime dt)
        {
            return dt.Date.AddMonths(-1).StartOfMonth();
        }

        public static DateTime LastYear(this DateTime dt)
        {
            return dt.Date.AddYears(-1).StartOfYear();
        }
        #endregion
    }
}

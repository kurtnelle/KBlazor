using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KBlazor.Models
{
    public static class RelativeDateCalc
    {
        public static string[] PartA = new string[] { "Today", "This", "Last", "Next" };
        public static string[] PartC = new string[] { "Week", "Month", "Year" };

        public static bool IsValidRelativeDate(string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                var parts = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var first = parts.FirstOrDefault();
                var last = parts.Length > 1 ? parts.LastOrDefault() : string.Empty;
                var xnumber = 0;
                last = int.TryParse(last, out xnumber) ? string.Empty : last;
                switch (first)
                {
                    case "Today":
                        return true;
                    case "This":
                    case "Last":
                    case "Next":
                        switch (last)
                        {
                            case "":
                            case "Week":
                            case "Month":
                            case "Year":
                                return true;
                            default:return false;
                        }
                    default:return false;
                }
            }
            else
            {
                return false;
            }
        }

        public static DateTime GetLowerDate(string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                var parts = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var now = DateTime.Now.Date;
                var first = parts.FirstOrDefault();
                if (parts.Length == 1 && first == "Today")
                {
                    return now;
                }
                else if (parts.Length == 2 && first == "This")
                {
                    switch (parts.LastOrDefault())
                    {
                        case "Week": return now.StartOfWeek();
                        case "Month": return now.StartOfMonth();
                        case "Year": return now.StartOfYear();
                        default: return DateTime.MinValue;
                    }
                }
                else if (parts.Length == 3)
                {
                    var xNumber = 0;
                    int.TryParse(parts.Skip(1).FirstOrDefault(), out xNumber);
                    var last = parts.LastOrDefault();
                    if (first == "Last")
                    {
                        switch (last)
                        {
                            case "Week": return now.AddDays(-7 * xNumber).StartOfWeek();
                            case "Month": return now.AddMonths(-1 * xNumber).StartOfMonth();
                            case "Year": return now.AddYears(-1 * xNumber).StartOfYear();
                            default: return DateTime.MinValue;
                        }
                    }
                    else if (first == "Next")
                    {
                        switch (last)
                        {
                            case "Week":
                            case "Month":
                            case "Year": return now;
                            default: return DateTime.MinValue;
                        }
                    }
                    else
                    {
                        return DateTime.MinValue;
                    }
                }
                else
                {
                    return DateTime.MinValue;
                }
            }
            else
            {
                return DateTime.MinValue;
            }
        }

        public static DateTime GetUpperDate(string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                var parts = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var now = DateTime.Now.Date;
                var first = parts.FirstOrDefault();
                if (parts.Length == 1 && first == "Today")
                {
                    return now.EndOfDay();
                }
                else if (parts.Length == 2 && first == "This")
                {
                    switch (parts.LastOrDefault())
                    {
                        case "Week": return now.EndOfWeek();
                        case "Month": return now.EndOfMonth();
                        case "Year": return now.EndOfYear();
                        default: return DateTime.MaxValue;
                    }
                }
                else if (parts.Length == 3)
                {
                    var xNumber = 0;
                    int.TryParse(parts.Skip(1).FirstOrDefault(), out xNumber);
                    var last = parts.LastOrDefault();
                    if (first == "Last")
                    {
                        switch (last)
                        {
                            case "Week":
                            case "Month":
                            case "Year":
                                return now.EndOfDay();
                            default: return DateTime.MaxValue;
                        }
                    }
                    else if (first == "Next")
                    {
                        switch (last)
                        {
                            case "Week": return now.AddDays(7 * xNumber).EndOfWeek();
                            case "Month": return now.AddMonths(xNumber).EndOfMonth();
                            case "Year": return now.AddYears(xNumber).EndOfYear();
                            default: return DateTime.MaxValue;
                        }
                    }
                    else
                    {
                        return DateTime.MaxValue;
                    }
                }
                else
                {
                    return DateTime.MaxValue;
                }
            }
            else
            {
                return DateTime.MaxValue;
            }
        }
    }
}

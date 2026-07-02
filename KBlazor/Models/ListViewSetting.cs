using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Drawing;
using System.Linq;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Threading.Tasks;
using System.Linq.Expressions;
using KBlazor.Attributes;

namespace KBlazor.Models
{
    [Display(Name = "List View Setting")]
    public class ListViewSetting : IKBusinessEntity
    {
        private static Type? ResolveType(string typeName)
        {
            return Type.GetType(typeName)
                ?? AppDomain.CurrentDomain.GetAssemblies()
                    .Select(a => a.GetType(typeName))
                    .FirstOrDefault(t => t != null);
        }

        protected string definition = string.Empty;
        protected List<PropertySetting> displaySettings = new List<PropertySetting>();
        [Key, ForeignKey("ListViewSettingId")]
        public Guid Id { get; set; }
        [Display(Name = "Name")]
        public string Name { get; set; } = string.Empty;

        public virtual ListViewSetting? ParentView { get; set; }
        [ForeignKey("ParentView")]
        public Guid? ParentViewId { get; set; }

        [Display(Name = "For Entity")]
        [ReadOnlyOnEdit]
        public string ForEntity { get; set; } = String.Empty;
        public string Definition { get; set; } = String.Empty;
        public virtual ICollection<ListViewSetting> Children { get; set; } = new List<ListViewSetting>();
        [Display(Name = "Page Size")]
        public int PageSize { get; set; }

        [NotMapped]
        public FlexTableViewMode ViewMode { get; set; } = FlexTableViewMode.Table;

        [NotMapped]
        public List<PropertySetting> DisplaySettings
        {
            get
            {
                return displaySettings;
            }
        }

        [NotMapped]
        public List<PropertySetting> AllDisplayableFields
        {
            get
            {
                return GetDefaultProperties("Helvetica Neue", 18.288f).ToList();
            }
        }

        [Display(Name = "Customized For User")]
        [ReadOnlyOnEdit]
        public string? CustomizedForUser { get; set; }

        [NotMapped]
        [JsonIgnore]
        public List<Guid> PinnedItemIds { get; set; } = new();

        public void UpdateDefinition()
        {
            var wrapper = new ViewDefinition
            {
                Columns = displaySettings,
                ViewMode = (int)ViewMode,
                PinnedItemIds = PinnedItemIds
            };
            Definition = JsonConvert.SerializeObject(wrapper);
        }

        public void InitilizeDefinition()
        {
            if (!string.IsNullOrEmpty(Definition))
            {
                // Backwards compatible: old format is a JSON array, new format is an object
                var trimmed = Definition.TrimStart();
                if (trimmed.StartsWith("["))
                {
                    // Old format: just a List<PropertySetting>
                    displaySettings = JsonConvert.DeserializeObject<List<PropertySetting>>(Definition) ?? new List<PropertySetting>();
                    ViewMode = FlexTableViewMode.Table;
                }
                else
                {
                    var wrapper = JsonConvert.DeserializeObject<ViewDefinition>(Definition);
                    displaySettings = wrapper?.Columns ?? new List<PropertySetting>();
                    ViewMode = (FlexTableViewMode)(wrapper?.ViewMode ?? 0);
                    PinnedItemIds = wrapper?.PinnedItemIds ?? new List<Guid>();
                }
                if (!string.IsNullOrEmpty(ForEntity) && !string.IsNullOrEmpty(Definition))
                {
                    var type = ResolveType(ForEntity);
                    foreach (var setting in displaySettings)
                    {
                        var property = type?.GetProperty(setting.Name);
                        if (property != null)
                        {
                            setting.PropertyInfo = property;
                            // Backfill DisplayName from [Display] attribute if missing from JSON
                            if (string.IsNullOrEmpty(setting.DisplayName))
                            {
                                setting.DisplayName = property.GetCustomAttribute<DisplayAttribute>()?.Name ?? property.Name;
                            }
                        }
                        else
                        {
                            throw new Exception($"While initilizing the definition for {ForEntity} the property {setting.Name}, could not be found");
                        }
                    }
                    displaySettings = displaySettings.Where(w => w.PropertyInfo != null).ToList();
                }
            }
        }
        public bool Equals(IKBusinessEntity? other)
        {
                return Id == other?.Id;
        }

        public override string ToString()
        {
            return Name;
        }

        public List<PropertySetting> GetDefaultProperties(string fontFamily, float fontSize)
        {
            List<PropertySetting> list = new List<PropertySetting>();

            var type = ResolveType(ForEntity);
            var properties = type.GetProperties()
                .Where(w => w.GetCustomAttributes(typeof(DisplayAttribute), false).Any())
                .OrderBy(o => (o.GetCustomAttributes(typeof(DisplayAttribute), false).FirstOrDefault() as DisplayAttribute).GetOrder())
                .ToList();

            properties.ForEach(f =>
            {
                list.Add(new PropertySetting()
                {
                    DisplayWidth = (int)f.DisplayNameOrDefault().GetTextSize(fontFamily, fontSize),
                    Id = Guid.NewGuid(),
                    Name = f.Name,
                    DisplayName = f.GetCustomAttribute<DisplayAttribute>().Name ?? f.Name,
                    PropertyInfo = f
                });
            });
            return list;
        }

        public List<PropertySetting> GetDefaultProperties(string fontFamily, float fontSize, string fields)
        {
            var list = new List<PropertySetting>();

            var type = ResolveType(ForEntity);
            var properties = type.GetProperties()
                .Where(w => w.GetCustomAttributes(typeof(DisplayAttribute), false).Any())
                .ToList();
            if (!string.IsNullOrEmpty(fields))
            {
                foreach (var field in fields.Split(","))
                {
                    var f = properties.Where(w => ((DisplayAttribute)w.GetCustomAttribute(typeof(DisplayAttribute))).Name == field).FirstOrDefault();
                    list.Add(new PropertySetting()
                    {
                        DisplayWidth = (int)f.DisplayNameOrDefault().GetTextSize(fontFamily, fontSize),
                        Id = Guid.NewGuid(),
                        Name = f.Name,
                        DisplayName = f.GetCustomAttribute<DisplayAttribute>().Name ?? f.Name,
                        PropertyInfo = f
                    });
                }
            }
            else
            {
                properties.ForEach(f => list.Add(new PropertySetting()
                {
                    DisplayWidth = (int)f.DisplayNameOrDefault().GetTextSize(fontFamily, fontSize),
                    Id = Guid.NewGuid(),
                    Name = f.Name,
                    DisplayName = f.GetCustomAttribute<DisplayAttribute>()?.Name ?? f.Name,
                    PropertyInfo = f
                }));
            }
            return list;
        }


        public ListViewSetting Clone(string newName)
        {
            var clone = new ListViewSetting()
            {
                Name = newName,
                ForEntity = ForEntity,
                CustomizedForUser = CustomizedForUser,
                PageSize = PageSize,
                ParentView = this
            };
            clone.displaySettings = displaySettings;
            return clone;
        }
        public void ResetToParent()
        {
            if (ParentView != null)
            {
                PageSize = ParentView.PageSize;
                displaySettings.Clear();
                ParentView.InitilizeDefinition();
                displaySettings.AddRange(ParentView.displaySettings);
            }
        }
    }

    public class PropertySetting : IEquatable<PropertySetting>
    {
        protected string filter = string.Empty;
        public Guid Id { get; set; }

        [JsonIgnore]
        public PropertyInfo PropertyInfo { get; set; }
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public int DisplayWidth { get; set; }
        public string Filter
        {
            get
            {
                if (PropertyInfo?.PropertyType == typeof(DateTime))
                {
                    var result = $"{GreaterThanDate:yyyy-MM-dd},{LessThanDate:yyyy-MM-dd}";
                    return result != "," ? result : RelativeDateCalc.IsValidRelativeDate(filter) ? filter : string.Empty;
                }
                else if (PropertyInfo?.PropertyType == typeof(int))
                {
                    if (GreaterThanDouble == 0 && LessThanDouble == 0)
                    {
                        return string.Empty;
                    }
                    else
                    {
                        return $"{GreaterThanDouble},{LessThanDouble}";
                    }
                }
                else if (PropertyInfo?.PropertyType == typeof(TimeSpan))
                {
                    var result = $"{(GreaterThanTime.HasValue ? GreaterThanTime.Value.ToString() : string.Empty)},{(LessThanTime.HasValue ? LessThanTime.Value.ToString() : string.Empty)}";
                    return result != "," ? result : string.Empty;
                }
                else
                {
                    return filter;
                }
            }
            set
            {
                filter = value;
                if (PropertyInfo?.PropertyType == typeof(DateTime) && !string.IsNullOrEmpty(filter))
                {
                    var parts = filter.Split(',', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Count() == 2)
                    {
                        GreaterThanDate = DateTime.ParseExact(parts[0], "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
                        LessThanDate = DateTime.ParseExact(parts[1], "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
                    }
                    else if (RelativeDateCalc.IsValidRelativeDate(filter))
                    {
                    }
                    else
                    {
                        throw new ArgumentException("If the property type is a DateTime, then the filter string must have both dates specified or a one of the relative date strings");
                    }
                }
                else if (PropertyInfo?.PropertyType == typeof(int) && !string.IsNullOrEmpty(filter))
                {
                    var parts = filter.Split(',', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Count() == 2)
                    {
                        GreaterThanDouble = double.Parse(parts[0]);
                        LessThanDouble = double.Parse(parts[1]);
                    }
                    else
                    {
                        throw new ArgumentException("If the property type is Numeric, then the filter string must have an upper and lower double value");
                    }
                }
                else if (PropertyInfo?.PropertyType == typeof(TimeSpan) && !string.IsNullOrEmpty(filter))
                {
                    var parts = filter.Split(',', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Count() == 2)
                    {
                        GreaterThanTime = TimeSpan.Parse(parts[0]);
                        LessThanTime = TimeSpan.Parse(parts[1]);
                    }
                    else
                    {
                        throw new ArgumentException("If the property is TimeSpan, then the filter string must have an upper and lower TimeSpan value");
                    }
                }
                else if (string.IsNullOrEmpty(filter))
                {
                    GreaterThanDate = null;
                    LessThanDate = null;
                    GreaterThanDouble = 0;
                    LessThanDouble = 0;
                    GreaterThanTime = null;
                    LessThanTime = null;
                }
            }
        }

        /// <summary>
        /// True iff at least one filter dimension on this column carries a non-default value.
        /// Use this for "is there anything to clear?" checks; Filter alone is unreliable
        /// because double columns don't round-trip their numeric bounds through it.
        /// </summary>
        [JsonIgnore]
        public bool HasActiveFilter =>
            !string.IsNullOrEmpty(filter)
            || GreaterThanDate.HasValue || LessThanDate.HasValue
            || GreaterThanDouble != 0   || LessThanDouble != 0
            || GreaterThanTime.HasValue || LessThanTime.HasValue;

        public Guid[] GetFilterEntries() => Filter.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Select(s => Guid.Parse(s)).ToArray();
        public DateTime? GreaterThanDate { get; set; }
        public DateTime? LessThanDate { get; set; }

        public TimeSpan? GreaterThanTime { get; set; }
        public TimeSpan? LessThanTime { get; set; }

        public double GreaterThanDouble { get; set; }
        public double LessThanDouble { get; set; }

        public SortState SortState { get; set; }
        public int SortPriority { get; set; } = 0;

        [JsonIgnore]
        public bool FilterDialogIsOpen { get; set; } = false;

        [JsonIgnore]
        public object BaseMatMenu { get; set; }
        [JsonIgnore]
        public object MenuButton { get; set; }

        [JsonIgnore]
        public string AutoCompleteText { get; set; }

        [JsonIgnore]
        public string EntitySearchText { get; set; }
        void AddStringToFilter(string value)
        {
            filter = string.Join(',', filter
                .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .ToArray()
                .Append(value));
        }
        void RemoveStringFromFilter(string value)
        {
            filter = string.Join(',', filter
                .Replace(value, string.Empty)
                .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries));
        }
        public void AddToFilter(int value) => AddStringToFilter(value.ToString());
        public void RemoveFromFilter(int value) => RemoveStringFromFilter(value.ToString());
        public void AddToFilter(Guid value) => AddStringToFilter(value.ToString());
        public void RemoveFromFilter(Guid value) => RemoveStringFromFilter(value.ToString());

        /// <summary>
        /// Use for primitives only
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="query"></param>
        /// <returns></returns>
        public IQueryable<T> GenerateWhere<T>(IQueryable<T> query)
        {
            if (PropertyInfo.PropertyType == typeof(string))
            {
                return query.Where(PropertyInfo, Filter);
            }
            else if (PropertyInfo.PropertyType == typeof(DateTime))
            {
                if (RelativeDateCalc.IsValidRelativeDate(Filter))
                {

                    return query.Where(PropertyInfo, RelativeDateCalc.GetLowerDate(Filter), RelativeDateCalc.GetUpperDate(Filter));
                }
                else
                {
                    return query.Where(PropertyInfo,
                        GreaterThanDate.HasValue ? GreaterThanDate.Value : DateTime.MinValue,
                        LessThanDate.HasValue ? LessThanDate.Value : DateTime.MaxValue);
                }
            }
            else if (PropertyInfo.PropertyType == typeof(double))
            {
                return query.Where(PropertyInfo, GreaterThanDouble, LessThanDouble);
            }
            else if (PropertyInfo.PropertyType == typeof(int))
            {
                return query.Where(PropertyInfo, (int)GreaterThanDouble, (int)LessThanDouble);
            }
            else if (PropertyInfo.PropertyType == typeof(bool))
            {
                return query.Where(PropertyInfo, bool.Parse(filter));
            }
            else if (PropertyInfo.PropertyType.IsEnum)
            {
                int[] enumEntries = Filter.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Select(s => int.Parse(s)).ToArray();
                return query.Where(PropertyInfo, enumEntries);
            }
            else
            {
                return query;
            }

        }
        public IQueryable<T> GenerateOrderBy<T>(IQueryable<T> query)
        {
            var param = Expression.Parameter(typeof(T), "x");
            var property = Expression.Property(param, Name);
            var lambda = Expression.Lambda(property, param);

            string methodName = SortState == SortState.Up ? "OrderBy" : "OrderByDescending";
            var method = typeof(Queryable).GetMethods()
                .Where(m => m.Name == methodName && m.GetParameters().Length == 2)
                .Single()
                .MakeGenericMethod(typeof(T), property.Type);

            return (IQueryable<T>)method.Invoke(null, new object[] { query, lambda });
        }



        public bool Equals(PropertySetting other)
        {
            return PropertyInfo.Name == other.PropertyInfo.Name &&
                PropertyInfo.DeclaringType.FullName == other.PropertyInfo.DeclaringType.FullName;
        }
    }

    public enum SortState : int
    {
        None = 0,
        Up = 1,
        Down = 2
    }

    public static class SupportedRelativeDates
    {
        static Dictionary<string, DatePair> values = new Dictionary<string, DatePair>();
        public static IEnumerable<string> Values => values.Keys;
        static SupportedRelativeDates()
        {
            values.Add("Today", new DatePair() { GreaterThanValue = new Func<DateTime>(() => DateTime.Now.Date), LessThanValue = new Func<DateTime>(() => DateTime.Now.EndOfDay()) });
            values.Add("This Week", new DatePair() { GreaterThanValue = new Func<DateTime>(() => DateTime.Now.StartOfWeek()), LessThanValue = new Func<DateTime>(() => DateTime.Now.EndOfWeek()) });
            values.Add("This Month", new DatePair() { GreaterThanValue = new Func<DateTime>(() => DateTime.Now.StartOfMonth()), LessThanValue = new Func<DateTime>(() => DateTime.Now.EndOfMonth()) });
            values.Add("This Year", new DatePair() { GreaterThanValue = new Func<DateTime>(() => DateTime.Now.StartOfYear()), LessThanValue = new Func<DateTime>(() => DateTime.Now.EndOfYear()) });
            values.Add("Yesterday", new DatePair() { GreaterThanValue = new Func<DateTime>(() => DateTime.Now.Yesterday()), LessThanValue = new Func<DateTime>(() => DateTime.Now.Yesterday().EndOfDay()) });
            values.Add("Last Week", new DatePair() { GreaterThanValue = new Func<DateTime>(() => DateTime.Now.LastWeek()), LessThanValue = new Func<DateTime>(() => DateTime.Now.LastWeek().EndOfWeek()) });
            values.Add("Last Month", new DatePair() { GreaterThanValue = new Func<DateTime>(() => DateTime.Now.LastMonth()), LessThanValue = new Func<DateTime>(() => DateTime.Now.LastMonth().EndOfMonth()) });
            values.Add("Last Year", new DatePair() { GreaterThanValue = new Func<DateTime>(() => DateTime.Now.LastYear()), LessThanValue = new Func<DateTime>(() => DateTime.Now.LastYear().EndOfYear()) });
        }

        public static Func<DateTime> GreaterThanValue(string key)
        {
            return values[key].GreaterThanValue;
        }
        public static Func<DateTime> LessThanValue(string key)
        {
            return values[key].LessThanValue;
        }

        class DatePair
        {
            public Func<DateTime> GreaterThanValue { get; set; }
            public Func<DateTime> LessThanValue { get; set; }
        }
    }

    internal class ViewDefinition
    {
        public List<PropertySetting> Columns { get; set; } = new();
        public int ViewMode { get; set; } = 0;
        public List<Guid> PinnedItemIds { get; set; } = new();
    }
}

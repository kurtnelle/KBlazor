using Microsoft.AspNetCore.Components;
using KBlazor.Models;
using KBlazor.Attributes;
using KBlazor.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace KBlazor.Components
{
    public partial class BasicEdit<TItem> where TItem : class
    {
        string headerText = typeof(TItem)
            .GetCustomAttributes(typeof(DisplayAttribute), false)
            .Cast<DisplayAttribute>()
            .FirstOrDefault()?.Name ?? typeof(TItem).Name;

        [Parameter]
        public TItem Item { get; set; }

        [Parameter]
        public Action Save { get; set; }

        [Parameter]
        public Action Close { get; set; }

        [Parameter]
        public int Columns { get; set; } = 1;

        [Parameter]
        public bool IsValid { get; set; } = true;

        [Parameter]
        public string Fields { get; set; }


        List<PropertyInfo> propertyList = (typeof(TItem)
            .GetProperties()
            .Where(w => w.GetCustomAttribute(typeof(DisplayAttribute)) != null)
            .ToList());

        Dictionary<PropertyInfo, bool> memoDialogIsOpened = (typeof(TItem)
            .GetProperties()
            .Where(w => w.GetCustomAttribute(typeof(MemoDisplayAttribute)) != null)
            .ToDictionary(d => d, d => false));

        Dictionary<string, IKBusinessEntity> PropertyCache = new Dictionary<string, IKBusinessEntity>();

        protected void SetScopeFor(string property, IKBusinessEntity entity)
        {
            if (!PropertyCache.ContainsKey(property))
            {
                PropertyCache.Add(property, entity);
            }
            else
            {
                PropertyCache[property] = entity;
            }
        }

        protected String FormatCamlCase(string value)
        {
            StringBuilder builder = new StringBuilder();
            foreach (char c in value)
            {
                if (Char.IsUpper(c) && builder.Length > 0)
                {
                    builder.Append(' ');
                }
                builder.Append(c);
            }
            return builder.ToString();
        }
    }
}

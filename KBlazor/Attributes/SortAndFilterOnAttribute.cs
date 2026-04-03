using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace KBlazor.Attributes
{
    public class SortAndFilterOnAttribute: Attribute
    {
        public string Member { get; set; }
        public string SortPath { get; set; }
        public string FilterPath { get; set; }
    }
}

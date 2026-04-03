using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KBlazor.Attributes
{
    public class AlsoIncludeAttribute :Attribute
    {
        public string Name { get; set; }
    }
}

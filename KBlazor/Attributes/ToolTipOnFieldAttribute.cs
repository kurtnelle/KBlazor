using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KBlazor.Attributes
{
    public class ToolTipAttribute: Attribute
    {
        public string PropertyName { get; set; }
    }
}

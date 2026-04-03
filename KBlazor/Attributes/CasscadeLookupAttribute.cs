using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace KBlazor.Attributes
{
    public class CasscadeLookupAttribute :Attribute
    {
    public string AdditionalProperties { get; set; }
    }
}

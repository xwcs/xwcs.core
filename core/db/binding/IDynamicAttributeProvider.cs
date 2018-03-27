using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace xwcs.core.db.binding
{
    public interface IDynamicAttributeProvider
    {
        IEnumerable<Attribute> GetAttributes(Type CurrentType, string PropertyName);
    }
}

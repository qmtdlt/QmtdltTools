using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QmtdltTools.Domain.Enums
{
    public enum VideoCollectionType:int
    {
        [Description("在线")]
        OnLine = 1,
        [Description("离线")]
        OffLine  =2
    }
}

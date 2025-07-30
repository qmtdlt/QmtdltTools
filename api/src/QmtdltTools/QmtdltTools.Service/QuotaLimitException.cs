using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QmtdltTools.Service
{
    public class QuotaLimitException : Exception
    {
        public string Action { get; }

        public QuotaLimitException(string action, string message) : base(message)
        {
            Action = action;
        }
    }
}

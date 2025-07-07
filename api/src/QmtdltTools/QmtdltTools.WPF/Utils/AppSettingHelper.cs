using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QmtdltTools.WPF.Utils
{
    public class AppSettingHelper
    {
        public static string lastUrl
        {
            get { return GetValue(nameof(lastUrl)); }
            set { SetValue(nameof(lastUrl), value.ToString()); }
        }
        public static string GetValue(string key, string value = default(string))
        {
            try
            {
                Configuration cfa = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

                return cfa.AppSettings.Settings[key].Value;
            }
            catch
            {
                SetValue(key, value);
                return value;
            }
        }

        public static void SetValue(string key, string value)
        {
            Configuration cfa = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            cfa.AppSettings.Settings[key].Value = value;

            cfa.Save();
        }
    }
}

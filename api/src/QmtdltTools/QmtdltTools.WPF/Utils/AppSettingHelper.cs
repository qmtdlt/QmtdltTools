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
        
        public static long LastVideoProgress
        {
            get { return long.Parse(GetValue(nameof(LastVideoProgress))); }
            set { SetValue(nameof(LastVideoProgress), value.ToString()); }
        }
        public static string LastVideoPath
        {
            get { return GetValue(nameof(LastVideoPath)); }
            set { SetValue(nameof(LastVideoPath), value.ToString()); }
        }
        public static string lastUrl
        {
            get { return GetValue(nameof(lastUrl)); }
            set { SetValue(nameof(lastUrl), value.ToString()); }
        }
        public static string ApiServer
        {
            get { return GetValue(nameof(ApiServer)); }
            set { SetValue(nameof(ApiServer), value.ToString()); }
        }
        public static string OnLineCfg
        {
            get { return GetValue(nameof(OnLineCfg)); }
            set { SetValue(nameof(OnLineCfg), value.ToString()); }
        }
        public static string OffLineCfg
        {
            get { return GetValue(nameof(OffLineCfg)); }
            set { SetValue(nameof(OffLineCfg), value.ToString()); }
        }
        #region GET SET
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
        #endregion
    }
}

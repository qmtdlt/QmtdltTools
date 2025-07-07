using QmtdltTools.Domain.Data;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.Settings;

namespace QmtdltTools.WPF
{
    public class MySettingProvider : SettingDefinitionProvider
    {
        public override void Define(ISettingDefinitionContext context)
        {
            context.Add(
                new SettingDefinition("SPEECH_KEY", ""),
                new SettingDefinition("SPEECH_REGION", ""),
                new SettingDefinition("GROK_KEY", ""),
                new SettingDefinition("GEMINI_KEY", ""),
                new SettingDefinition("DOU_BAO", ""),
                new SettingDefinition("QIAN_WEN", ""));
        }
    }
}

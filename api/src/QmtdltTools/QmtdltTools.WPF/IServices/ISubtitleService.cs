using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace QmtdltTools.WPF.IServices
{
    public interface ISubtitleService : ISingletonDependency
    {
        Task StartRecognizeAsync(Action<string> updating, Action<string> recoginized, CancellationToken cancellationToken = default);
        void Pause();
        void Resume();
        Task StopAsync();
    }
}

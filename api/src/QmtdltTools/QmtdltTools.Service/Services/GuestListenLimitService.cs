using QmtdltTools.Domain.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace QmtdltTools.Service.Services
{
    public class GuestListenLimitService:ISingletonDependency
    {
        public async Task<bool> IsGuestLimited(string guestId)
        {
            double used = await GetGuestListenSeconds(guestId);
            return used >= ApplicationConst.GuestListenTimeLimit;
        }

        public async Task<double> GetGuestListenSeconds(string guestId)
        {
            string key = BuildRedisKey(guestId);
            var value = await RedisHelper.GetAsync<double?>(key);
            return value??0;
        }

        public async Task AddGuestListenSeconds(string guestId, double seconds)
        {
            string key = BuildRedisKey(guestId);
            decimal increment = (decimal)seconds; // 显式转换
            decimal newTotal = await RedisHelper.IncrByFloatAsync(key, increment);

            // 设置月底过期
            var now = DateTime.UtcNow;
            var expireAt = new DateTime(now.Year, now.Month, 1).AddMonths(1); // 下月1日
            await RedisHelper.ExpireAtAsync(key, expireAt);
        }

        private string BuildRedisKey(string guestId)
        {
            string month = DateTime.UtcNow.ToString("yyyyMM");
            return $"GuestListen:{guestId}:{month}";
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace QmtdltTools.Service.Services
{
    public class GuestQuotaLimitService : ISingletonDependency
    {
        public async Task<bool> IsLimited(string userId, string action, int limit)
        {
            int used = await GetUsed(userId, action);
            return used >= limit;
        }

        public async Task<int> GetUsed(string userId, string action)
        {
            string key = BuildKey(userId, action);
            int value = await RedisHelper.GetAsync<int>(key);
            return value;
        }

        public async Task AddUsage(string userId, string action, int count = 1)
        {
            string key = BuildKey(userId, action);
            long newVal = await RedisHelper.IncrByAsync(key, count);

            // 设置月底过期
            var now = DateTime.UtcNow;
            var expireAt = new DateTime(now.Year, now.Month, 1).AddMonths(1);
            await RedisHelper.ExpireAtAsync(key, expireAt);
        }

        private string BuildKey(string userId, string action)
        {
            string month = DateTime.UtcNow.ToString("yyyyMM");
            return $"GuestQuota:{userId}:{month}:{action}";
        }
    }
}

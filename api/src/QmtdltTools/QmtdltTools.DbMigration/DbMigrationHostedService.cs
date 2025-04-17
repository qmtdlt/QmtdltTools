using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using QmtdltTools.Domain.Entitys;
using QmtdltTools.EFCore;
using Volo.Abp;

namespace QmtdltTools.DbMigration;

public class DbMigrationHostedService : IHostedService
{
    DC _dc;
    public DbMigrationHostedService(DC dc)
    {
        _dc = dc;
    }

    public async Task Start()
    {
        await _dc.Database.EnsureCreatedAsync();

        if (!_dc.SysUsers.Any())
        {
            // 添加种子数据
            _dc.SysUsers.AddRange(new[]
            {
                new SysUser
                {
                    Name = "qmtdlt",
                    Code = "qmtdlt",
                    Email = "qmtdlt@gmail.com",
                    PasswordHash = "12000asd", // 替换为实际的哈希密码
                    PhoneNumber = "13679112984",
                    IsActive = true
                }
            });

            // 保存更改
            await _dc.SaveChangesAsync();
        }
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await Start();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

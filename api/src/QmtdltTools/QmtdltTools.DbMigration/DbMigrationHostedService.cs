using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
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
        _dc.Database.EnsureCreatedAsync().Wait();
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

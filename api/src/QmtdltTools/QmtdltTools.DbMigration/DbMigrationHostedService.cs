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
        _= Start();
    }

    public async Task Start()
    {
        var res = _dc.SysUsers.AsQueryable().ToList();
        Console.WriteLine(res.Count);
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

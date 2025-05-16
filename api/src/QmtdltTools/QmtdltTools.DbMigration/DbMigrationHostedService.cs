using System;
using System.IO;
using System.Linq;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
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
    public async Task BakupData()
    {
        // 备份数据
        System.Collections.Generic.List<SysUser> users = await _dc.SysUsers.ToListAsync();
        System.Collections.Generic.List<EBookMain> ebooks = await _dc.EBooks.ToListAsync();
        System.Collections.Generic.List<ListenWriteRecord> listenwrite = await _dc.ListenWriteRecords.ToListAsync();
        System.Collections.Generic.List<VocabularyRecord> vocabulary = await _dc.VocabularyRecords.ToListAsync();
        System.Collections.Generic.List<UserVocabulary> userVocabularies = await _dc.UserVocabularies.ToListAsync();
        // 将数据备份到文件
        File.WriteAllText("users.json", System.Text.Json.JsonSerializer.Serialize(users));
        File.WriteAllText("ebooks.json", System.Text.Json.JsonSerializer.Serialize(ebooks));
        File.WriteAllText("listenwrite.json", System.Text.Json.JsonSerializer.Serialize(listenwrite));
        File.WriteAllText("vocabulary.json", System.Text.Json.JsonSerializer.Serialize(vocabulary));
        File.WriteAllText("uservocabularies.json", System.Text.Json.JsonSerializer.Serialize(userVocabularies));
    }
    public async Task Start()
    {
        await _dc.Database.EnsureCreatedAsync();
        // 加载数据
        Console.WriteLine("1. Do you want to backup data to json file? input y for yes.");
        var input = Console.ReadLine();
        if (input == "y")
        {
            await BakupData();
        }
        Console.WriteLine("2. Do you want to insert the data from json file to database? input y for yes.");
        input = Console.ReadLine();
        if (input == "y")
        {
            try
            {
                var list = System.Text.Json.JsonSerializer.Deserialize<System.Collections.Generic.List<SysUser>>(File.ReadAllText("users.json"));
                await _dc.SysUsers.AddRangeAsync(list);
                await _dc.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            // ebook
            try
            {
                var list = System.Text.Json.JsonSerializer.Deserialize<System.Collections.Generic.List<EBookMain>>(File.ReadAllText("ebooks.json"));
                await _dc.EBooks.AddRangeAsync(list);
                await _dc.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            // listenwrite
            try
            {
                var list = System.Text.Json.JsonSerializer.Deserialize<System.Collections.Generic.List<ListenWriteRecord>>(File.ReadAllText("listenwrite.json"));
                await _dc.ListenWriteRecords.AddRangeAsync(list);
                await _dc.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            // vocabulary
            try
            {
                var list = System.Text.Json.JsonSerializer.Deserialize<System.Collections.Generic.List<VocabularyRecord>>(File.ReadAllText("vocabulary.json"));
                await _dc.VocabularyRecords.AddRangeAsync(list);
                await _dc.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            try
            {
                var list = System.Text.Json.JsonSerializer.Deserialize<System.Collections.Generic.List<UserVocabulary>>(File.ReadAllText("uservocabularies.json"));
                await _dc.UserVocabularies.AddRangeAsync(list);
                await _dc.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
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

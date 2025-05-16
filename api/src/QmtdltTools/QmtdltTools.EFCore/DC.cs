using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using QmtdltTools.Domain.Entitys;
using Volo.Abp.Data;
using Volo.Abp.EntityFrameworkCore;

namespace QmtdltTools.EFCore;

[ConnectionStringName("Default")]
public class DC:AbpDbContext<DC>
{
    private readonly IConfiguration _configuration;
    public DC(DbContextOptions<DC> options,IConfiguration configuration):base(options)
    {
        _configuration = configuration;
        this.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
    }
    
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if(!optionsBuilder.IsConfigured)
        {
            var connectionString = _configuration.GetConnectionString("Default");
            var serverVersion = new MySqlServerVersion(new Version(8, 0, 0));
            optionsBuilder.UseMySql(connectionString, serverVersion);
        }
    }
    public DbSet<SysUser> SysUsers { get; set; }
    public DbSet<EBookMain> EBooks { get; set; }
    public DbSet<DayToDo> DayToDos { get; set; }
    public DbSet<YearToDo> YearToDos { get; set; }
    public DbSet<ListenWriteRecord> ListenWriteRecords { get; set; }
    public DbSet<UserVocabulary> UserVocabularies { get; set; }
    public DbSet<VocabularyRecord> VocabularyRecords { get; set; }
}
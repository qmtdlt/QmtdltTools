using QmtdltTools.EFCore;
using Volo.Abp.DependencyInjection;

namespace QmtdltTools.Service.Services;

public class TestService:ITransientDependency
{
    private readonly DC _dc;
    public TestService(DC dc)
    {
        _dc = dc;
    }
    public string? GetTest()
    {
        return _dc.SysUsers.First().Id.ToString();
    }
}
using Volo.Abp.DependencyInjection;

namespace QmtdltTools.Service.Services;

public class TestService:ITransientDependency
{
    public TestService()
    {
        
    }
    public string GetTest()
    {
        return "Test";
    }
}
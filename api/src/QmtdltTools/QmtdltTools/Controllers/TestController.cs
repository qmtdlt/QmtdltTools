using Microsoft.AspNetCore.Mvc;
using QmtdltTools.Service.Services;
using Volo.Abp.AspNetCore.Mvc;

namespace QmtdltTools.Controllers;

[Route("api/[controller]/[action]")]
public class TestController : AbpController
{
    private readonly TestService _testService;
    public TestController(TestService testService)
    {
        _testService = testService;
    }
    // GET
    [HttpGet("get11")]
    public string Get()
    {
        return "Hello World!";
    }

    [HttpPost("post22")]
    public string Post(string input)
    {
        Console.WriteLine($"Hello World!{input}");
        return input + "returned" + _testService.GetTest();
    }
}
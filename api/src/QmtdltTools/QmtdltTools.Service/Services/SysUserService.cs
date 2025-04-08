using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Query.Internal;
using QmtdltTools.Domain.Entitys;
using QmtdltTools.Domain.Models;
using QmtdltTools.EFCore;
using Volo.Abp.DependencyInjection;

namespace QmtdltTools.Service.Services
{
    public class SysUserService: ITransientDependency
    {
        private readonly DC _dc;
        public SysUserService(DC dc)
        {
            _dc = dc;
        }
        //public async Task<Response<bool>> AddUser(string userName, string password)
        //{
        //    var user = new SysUser
        //    {
        //        UserName = userName,
        //        Password = password
        //    };
        //    _dc.SysUsers.Add(user);
        //    await _dc.SaveChangesAsync();
        //    return new Response<bool>
        //    {
        //        code = 0,
        //        message = "添加成功"
        //    };
        //}
    }
}

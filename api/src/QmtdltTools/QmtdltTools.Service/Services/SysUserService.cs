using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
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
        public async Task<Response<bool>> Delete(Guid id)
        {
            var user = await _dc.SysUsers.FindAsync(id);
            if (user == null)
            {
                return new Response<bool>
                {
                    code = 1,
                    message = "用户不存在"
                };
            }
            _dc.SysUsers.Remove(user);
            await _dc.SaveChangesAsync();
            return new Response<bool>
            {
                code = 0,
                message = "删除成功"
            };
        }
        // check login
        public async Task<Response<bool>> CheckLogin(string username, string password)
        {
            var user = await _dc.SysUsers.Where(t => t.Name == username && t.PasswordHash == password).FirstOrDefaultAsync();
            if (user == null)
            {
                return new Response<bool>
                {
                    code = 1,
                    message = "用户名或密码错误"
                };
            }
            return new Response<bool>
            {
                code = 0,
                message = "登录成功"
            };
        }
        // Register
        public async Task<Response<bool>> Register(SysUser user)
        {
            var existingUser = await _dc.SysUsers.Where(t => t.Name == user.Name).FirstOrDefaultAsync();
            if (existingUser != null)
            {
                return new Response<bool>
                {
                    code = 1,
                    message = "用户名已存在"
                };
            }
            user.Id = Guid.NewGuid();
            _dc.SysUsers.Add(user);
            await _dc.SaveChangesAsync();
            return new Response<bool>
            {
                code = 0,
                message = "注册成功"
            };
        }
    }
}

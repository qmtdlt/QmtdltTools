using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QmtdltTools.Domain.Models;

namespace QmtdltTools.EFCore.Extensions
{
    public static class PageExtension
    {
        public static async Task<PageResult<T>> ToPageList<T>(this IQueryable<T> source, int pageIndex, int pageSize) where T : class
        {
            var total = source.Count();
            var pageCount = (int)Math.Ceiling((double)total / pageSize);
            var pageList = await source.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToListAsync();
            return new PageResult<T>
            {
                PageList = pageList,
                Total = total,
                PageCount = pageCount
            };
        }

        public static async Task<PageResult<T>> ToPageList<T>(this DbSet<T> source, int pageIndex, int pageSize) where T : class
        {
            var total = source.Count();
            var pageCount = (int)Math.Ceiling((double)total / pageSize);
            var pageList = await source.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToListAsync();
            return new PageResult<T>
            {
                PageList = pageList,
                Total = total,
                PageCount = pageCount
            };
        }
    }
}

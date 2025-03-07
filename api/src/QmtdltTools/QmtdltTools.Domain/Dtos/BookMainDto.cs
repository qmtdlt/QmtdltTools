using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QmtdltTools.Domain.Dtos
{
    public class BookMainDto
    {
        public string? Title { get; set; }
        public string? Author { get; set; }
        public string? Description { get; set; }
        public byte[]? CoverImage { get; set; }
        public byte[] BookBin { get; set; }
    }
}

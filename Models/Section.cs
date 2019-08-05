using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace FileUpload.Models
{
    public class Section
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public byte[] Size { get; set; }
        public byte[] Vaddr { get; set; }
        public byte[] Total_sz { get; set; }
        public byte[] content { get; set; }
    }
}

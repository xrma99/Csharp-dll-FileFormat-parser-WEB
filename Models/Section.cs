using Microsoft.CodeAnalysis.CSharp.Syntax;
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

        [Display(Name="Valid Size")]
        public byte[] Size { get; set; }
        
        public byte[] Vaddr { get; set; }

        [Display(Name="Total Size")]
        public byte[] Total_sz { get; set; }

        [DataType(DataType.MultilineText)]
        public byte[] content { get; set; }

        
    }
    
}

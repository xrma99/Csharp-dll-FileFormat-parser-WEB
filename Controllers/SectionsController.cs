using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using FileUpload.Models;
using Microsoft.AspNetCore.Http;


using System.IO;
using PdfSharp.Pdf;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.ViewEngines;

namespace FileUpload.Controllers
{
    public class SectionsController : Controller
    {
        private readonly FileUploadContext _context;

        public SectionsController(FileUploadContext context)
        {
            _context = context;
        }

        public static byte[] Reverseread(byte[] data, int lowindex, int largeindex)
        {
            int i;
            byte tmp;
            for (i = lowindex; i <= (lowindex + largeindex) / 2; i++)
            {
                tmp = data[i];
                data[i] = data[largeindex - i];
                data[largeindex - i] = tmp;
            }

            return data;
        }

        private static int Calculate(byte[] data, int lowindex, int largeindex)
        {
            //length is no bigger than 4 bytes
            int res = 0;
            for (int i = lowindex; i <= largeindex; i++)
            {
                res = res * 256 + data[i];
            }
            return res;

        }

        public void analyze(string filepath)
        {
            ViewData["title"] = "Result";

            FileStream fs = new FileStream(filepath, FileMode.Open);
            BinaryReader br = new BinaryReader(fs);
            Byte content;
            char c;
            long totallen = br.BaseStream.Length;//total length

            int border = 0;
            int flag = 0;
            int paragraph_sz = 16;//unit:byte
            int section_c = 7;//一共有多少个section，最多有7个
            string[] section_name = new string[7];
            int[] section_sz = new int[7];
            int[] total_sz = new int[7];//.text .rsrc .reloc

            while (br.BaseStream.Position < br.BaseStream.Length)
            {
                switch (flag)
                {
                    case 0://MZ header
                        ViewBag.MZid = br.ReadBytes(2);

                        br.ReadBytes(6);

                        ViewBag.MZsize = Reverseread(br.ReadBytes(2), 0, 1);//get the length of the MZ header
                        int len = Calculate(ViewBag.MZsize, 0, 1);

                        br.ReadBytes(len * paragraph_sz - 10);

                        flag++;
                        break;

                    case 1://default part --- 64 bytes
                        br.ReadBytes(14);

                        string info = string.Empty;
                        for (int i = 0; i < 39; i++)
                        {
                            content = br.ReadByte();
                            c = (char)content;
                            info = info + c;
                            //为啥不能用append呢……QAQ
                        }//This program cannot be run in DOS mode.
                        ViewBag.info = info;
                        br.ReadBytes(11);

                        flag++;
                        break;
                    case 2://PE Header

                        ViewBag.PEid = br.ReadBytes(4);

                        ViewBag.MachineType = Reverseread(br.ReadBytes(2), 0, 1);

                        ViewBag.sectionamount = Reverseread(br.ReadBytes(2), 0, 1);//get the length of the MZ header
                        section_c = Calculate(ViewBag.sectionamount, 0, 1);

                        br.ReadBytes(240);

                        /*Clear the dbcontext, sectioncount is 7 in maximum*/
                        int sectioncount = _context.Section.Count();
                        for(int i = 0; i < sectioncount; i++)
                        {
                            var Dsection = _context.Section.First();
                            _context.Section.Remove(Dsection);
                            _context.SaveChanges();
                        }


                        flag++;
                        break;
                    case 3://section header information
                        info = string.Empty;
                        for (int i = 0; i < 8; i++)
                        {
                            content = br.ReadByte();
                            if (content != 0)
                            {
                                c = (char)content;
                                info = info + c;
                            }
                        }
                        section_name[border] = info;
                        var newsection = new Section
                        {
                            Name = info,
                            Size = Reverseread(br.ReadBytes(4), 0, 3),
                            Vaddr = Reverseread(br.ReadBytes(4), 0, 3),
                            Total_sz = Reverseread(br.ReadBytes(4), 0, 3)
                        };

                        section_sz[border] = Calculate(newsection.Size, 0, 3);
                        total_sz[border] = Calculate(newsection.Total_sz, 0, 3);

                        br.ReadBytes(20);

                        _context.Section.Add(newsection);
                        _context.SaveChanges();

                        border++;
                        if (border >= section_c)//section header 结束
                        {
                            flag++;
                            br.ReadBytes(16);//Reserved
                        }

                        break;
                    case 4://Section Content
                        for (int i = 0; i < section_c; i++)
                        {

                            var Thesection = _context.Section.First(a => a.Name == section_name[i]);
                            byte[] Thecontent = br.ReadBytes(section_sz[i]);
                            Thesection.content = Thecontent;
                            _context.SaveChanges();
                            br.ReadBytes(total_sz[i] - section_sz[i]);
                        }
                        flag++;
                        break;
                    default:
                        br.ReadByte();
                        break;
                }

            }

            br.Close();
            fs.Close();

        }

        // GET: Sections
        [HttpPost("Sections")]
        public async Task<IActionResult> Index(IFormFile file)

        {
            string filename = string.Empty;
            ViewData["name"] = file.FileName;
            if (file != null)
            {
                filename = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                string SavePath = Path.Combine(Directory.GetCurrentDirectory(), @"wwwroot\dllfiles", filename);
                using (var stream = new FileStream(SavePath, FileMode.Create))
                {
                    file.CopyTo(stream);
                }
                analyze(SavePath);
                System.IO.File.Delete(SavePath);

                return View(await _context.Section.ToListAsync());
            }
            return  RedirectToAction("Error");

        }

        public IActionResult PDFguide(string srcurl)
        {
            /*
            Byte[] res = null;
            using (MemoryStream ms=new MemoryStream())
            {
                var pdf = TheArtOfDev.HtmlRenderer.PdfSharp.PdfGenerator.GeneratePdf(srcurl, PdfSharp.PageSize.A4);
                pdf.Save(ms);
                res = ms.ToArray();
                return pdf;
            }
            //return HtmlEncoder.Default.Encode($"Hello {srcurl}");
            */
            return View();
            
            
        }

        public string ExportPDF(string srcurl)
        {
            return HtmlEncoder.Default.Encode($"Hello {srcurl}, It\'s not the real one :)");
        }

        public IActionResult Error()
        {
            return View();
        }

        // GET: Sections/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var section = await _context.Section
                .FirstOrDefaultAsync(m => m.Id == id);
            if (section == null)
            {
                return NotFound();
            }

            return View(section);
        }       
       
        private bool SectionExists(int id)
        {
            return _context.Section.Any(e => e.Id == id);
        }
    }
}

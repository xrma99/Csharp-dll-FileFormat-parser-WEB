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

        public void analyze(string filename)
        {
            ViewData["title"] = "Result";

            FileStream fs = new FileStream(@".\wwwroot\dllfiles\CsharpHelloworld.dll", FileMode.Open);
            BinaryReader br = new BinaryReader(fs);
            Byte content;
            char c;
            long totallen = br.BaseStream.Length;//total length

            int border = 0;
            int flag = 0;
            int paragraph_sz = 16;//unit:byte
            int section_c = 7;//一共有多少个section，最多有7个
            int[] section_sz = new int[7];
            int[] total_sz = new int[7];//.text .rsrc .reloc
            byte[] tmp = new byte[4];


            while (br.BaseStream.Position < br.BaseStream.Length)
            {
                switch (flag)
                {
                    case 0://MZ header
                        ViewBag.MZid = br.ReadBytes(2);

                        br.ReadBytes(6);

                        ViewBag.MZsize = tmp = Reverseread(br.ReadBytes(2), 0, 1);//get the length of the MZ header
                        int len = Calculate(tmp, 0, 1);

                        br.ReadBytes(len * paragraph_sz - 10);

                        flag++;
                        break;

                    case 1://default part
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

                        ViewBag.sectionamount = tmp = Reverseread(br.ReadBytes(2), 0, 1);//get the length of the MZ header
                        section_c = Calculate(tmp, 0, 1);

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
                        var newsection = new Section
                        {
                            Name = info,
                            Size = Reverseread(br.ReadBytes(4), 0, 3),
                            Vaddr = Reverseread(br.ReadBytes(4), 0, 3),
                            Total_sz = Reverseread(br.ReadBytes(4), 0, 3)
                        };

                        tmp = newsection.Size;
                        section_sz[border] = Calculate(tmp, 0, 3);
                        tmp = newsection.Total_sz;
                        total_sz[border] = Calculate(tmp, 0, 3);

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
                            
                            var Thesection = _context.Section.First(a => a.Id == i);
                            byte[] Thecontent = br.ReadBytes(section_sz[i]);
                            Thesection.content = Thecontent;
                            _context.SaveChanges();
                            br.ReadBytes(total_sz[i] - section_sz[i]);
                        }
                        flag++;
                        break;
                    default:
                        Console.Write(br.ReadByte().ToString("X"));
                        break;
                }

            }

            br.Close();
            fs.Close();

        }



        // GET: Sections
        public async Task<IActionResult> Index(IFormFile file)
        {
            string filename = string.Empty;
            if (file != null)
            {
                filename = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                string SavePath = Path.Combine(Directory.GetCurrentDirectory(), @"wwwroot\images", filename);
                using (var stream = new FileStream(SavePath, FileMode.Create))
                {
                    file.CopyTo(stream);
                }
                return View();
            }
            analyze(filename);
            return View(await _context.Section.ToListAsync());
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

        // GET: Sections/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Sections/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,Size,Vaddr,Total_sz,content")] Section section)
        {
            if (ModelState.IsValid)
            {
                _context.Add(section);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(section);
        }

        // GET: Sections/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var section = await _context.Section.FindAsync(id);
            if (section == null)
            {
                return NotFound();
            }
            return View(section);
        }

        // POST: Sections/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Size,Vaddr,Total_sz,content")] Section section)
        {
            if (id != section.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(section);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SectionExists(section.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(section);
        }

        // GET: Sections/Delete/5
        public async Task<IActionResult> Delete(int? id)
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

        // POST: Sections/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var section = await _context.Section.FindAsync(id);
            _context.Section.Remove(section);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool SectionExists(int id)
        {
            return _context.Section.Any(e => e.Id == id);
        }
    }
}

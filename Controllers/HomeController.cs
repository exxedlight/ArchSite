using ArchSite.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using IOf = System.IO.File;

namespace ArchSite.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Home()
        {
            return View();
        }
        public IActionResult Plan()
        {
            List<ModuleInfoModel> modules = new List<ModuleInfoModel>();

            List<string> files = Directory.GetFiles(Path.Combine("wwwroot", "textdata")).ToList();
            //files.ForEach(x => x = x.Split(Path.DirectorySeparatorChar).Last());

            foreach (string file in files)
            {
                try
                {
                    using (StreamReader reader = new StreamReader(file))
                    {
                        ModuleInfoModel module = new ModuleInfoModel();
                        module.module_name = reader.ReadLine();

                        while (reader.Peek() != -1)
                        {
                            module.themes.Add(reader.ReadLine());
                        }
                        modules.Add(module);
                    }
                }
                catch
                {
                    System.IO.File.WriteAllText(file, "Unknown Module\n[No data]");
                    continue;
                }
            }

            return View(modules);
        }

        public IActionResult Materials()
        {
            return Filter("1");
        }

        public IActionResult Filter(string type)
        {
            List<FileModel> files = getFilteredFiles(type);
            ViewData["select"] = type == "rest" ? "Інше" : "Модуль " + type;
            return View("Materials", files);
        }

        public static List<FileModel> getFilteredFiles(string place)
        {
            List<FileModel> model = new List<FileModel>();

            string path = Path.Combine("wwwroot", "filesdata", place == "rest" ? place : "mod" + place);
            if (!IOf.Exists(Path.Combine(path, "map.json")))
            {
                AdminController.RemapFiles(path);
            }

            model = JsonSerializer.Deserialize<List<FileModel>>(IOf.ReadAllText(Path.Combine(path, "map.json"))).ToList();

            model = model.OrderBy(x => x.WorkType).ToList();

            return model;
        }


        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}

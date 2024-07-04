using ArchSite.Models;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Text.Json;

using IOf = System.IO.File;

namespace ArchSite.Controllers
{

    public class AdminController : Controller
    {
        [HttpPost]
        public IActionResult EnterAdmin(string pass)
        {
            if(pass != "123")
            {
                return RedirectToAction("Home", "Home");
            }

            Response.Cookies.Append("adm", "true", new CookieOptions
            {
                Expires = DateTime.Now.AddDays(7)
            });

            return RedirectToAction("AdminPage");
        }

        public IActionResult AdminPage()
        {
            if (Request.Cookies["adm"] != "true")
            {
                return RedirectToAction("Home", "Home");
            }
            return Filter("1");
        }

        [HttpPost]
        public IActionResult AddFile(string worktype,
            string filename,
            string place,
            IFormFile file)
        {

            if (string.IsNullOrWhiteSpace(worktype) || string.IsNullOrWhiteSpace(filename) ||
                string.IsNullOrWhiteSpace(place) || file == null)
            {
                ViewData["Err"] = "Помилка отримання даних";
                return View("AdminPage");
            }

            string dir_path = Path.Combine("wwwroot", "filesdata", place == "rest" ? place : "mod" + place);

            List<FileModel> files_json = new List<FileModel>();
            RemapFiles(dir_path);


            try
            {
                files_json = JsonSerializer.Deserialize<List<FileModel>>(IOf.ReadAllText(Path.Combine(dir_path, "map.json"))).ToList();

                string newFileName = worktype + "~" + filename + Path.GetExtension(file.FileName);
                using (var stream = new FileStream(Path.Combine(dir_path, newFileName), FileMode.Create))
                {
                    file.CopyTo(stream);
                }

                FileModel model = new FileModel
                {
                    Filename = filename,
                    WorkType = worktype,
                    FilePath = Path.Combine(dir_path, newFileName)
                };
                files_json.Add(model);

                IOf.WriteAllText(Path.Combine(dir_path, "map.json"), JsonSerializer.Serialize(files_json, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch
            {
                if (!Directory.Exists(dir_path)) Directory.CreateDirectory(dir_path);
                if (!IOf.Exists(Path.Combine(dir_path, "map.json"))) IOf.WriteAllText(Path.Combine(dir_path, "map.json"), "[]");
                
                ViewData["Err"] = "Помилка даних, спробуйте ще раз";
                ViewData["select"] = place == "rest" ? "Інше" : "Модуль " + place;
                return View("AdminPage", HomeController.getFilteredFiles(place));
            }

            RemapFiles(dir_path);
            ViewData["select"] = place == "rest" ? "Інше" : "Модуль " + place;
            return View("AdminPage", HomeController.getFilteredFiles(place));
        }

        public static void RemapFiles(string dir_path)
        {

            if (!Directory.Exists(dir_path))
            {
                Directory.CreateDirectory(dir_path);
                IOf.WriteAllText(Path.Combine(dir_path, "map.json"), "[]");
                return;
            }

            List<string> files = Directory.GetFiles(dir_path).ToList();
            files.RemoveAll(x => x.Contains("map.json"));

            List<string> filenames = files.Select(x => x = x.Split(Path.DirectorySeparatorChar).Last()).ToList();

            List<FileModel> map = new List<FileModel>();
            for (int i = 0; i < files.Count; i++)
            {
                FileModel model = new FileModel
                {
                    Filename = filenames[i].Split('~').Last(),
                    FilePath = files[i].Replace("wwwroot", "").Replace(Path.DirectorySeparatorChar, '/'),
                    WorkType = filenames[i].Split('~').First()
                };

                map.Add(model);
            }
            IOf.WriteAllText(Path.Combine(dir_path, "map.json"), JsonSerializer.Serialize(map, new JsonSerializerOptions { WriteIndented = true }));
        }

        public IActionResult Filter(string type)
        {

            List<FileModel> files = HomeController.getFilteredFiles(type);
            ViewData["select"] = type == "rest" ? "Інше" : "Модуль "+type;
            return View("AdminPage", files);

        }

        [HttpPost]
        public IActionResult DeleteFile(string path)
        {
            path = path.Replace('/', Path.DirectorySeparatorChar);
            path = "wwwroot" + path;

            List<string> parts = path.Split(Path.DirectorySeparatorChar).ToList();

            string dir_path = path.Replace(
                Path.DirectorySeparatorChar + path.Split(Path.DirectorySeparatorChar).Last(), "");

            IOf.Delete(path);
            RemapFiles(dir_path);

            string select = dir_path.Split(Path.DirectorySeparatorChar).Last();

            ViewData["select"] = select == "rest" ? "Інше" : "Модуль " + select.Last();

            return Filter(select == "rest" ? select : select.Last().ToString());

        }
    }
}

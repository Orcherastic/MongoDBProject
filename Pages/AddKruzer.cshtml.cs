using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MongoDB.Driver;
using PROJEKAT_MONGODB.Model;
using Microsoft.AspNetCore.Http;
using System.IO;

namespace PROJEKAT_MONGODB.Pages
{
    public class AddKruzerModel : PageModel
    {
        private IWebHostEnvironment _environment;
        private readonly IMongoCollection<Korisnik> _dbKorisnici;
        private readonly IMongoCollection<Kruzer> _dbKruzeri;
        private readonly IMongoCollection<Kabina> _dbKabine;
        private readonly IMongoCollection<Grad> _dbGradovi;
        private readonly IMongoCollection<Drzava> _dbDrzave;
        public AddKruzerModel(IWebHostEnvironment ev, IDatabaseSettings settings)
        {
            _environment = ev;
            var client = new MongoClient("mongodb://localhost/?safe=true");
            var database = client.GetDatabase("SEVENSEAS");
            _dbKorisnici = database.GetCollection<Korisnik>("korisnici");
            _dbKruzeri = database.GetCollection<Kruzer>("kruzeri");
            _dbKabine = database.GetCollection<Kabina>("kabine");
            _dbGradovi = database.GetCollection<Grad>("gradovi");
            _dbDrzave = database.GetCollection<Drzava>("drzave");


        }
        [BindProperty]
        public string glavnaSlika { get; set; }
        [BindProperty]
        public string slika1 { get; set; }
        [BindProperty]
        public string slika2 { get; set; }
        [BindProperty]
        public string slika3 { get; set; }
        [BindProperty]
        public Kruzer noviKruzer { get; set; }
        [BindProperty]
        public List<string> kabine { get; set; }
        [BindProperty]
        public string cities { get; set; }
        [BindProperty]
        public string states { get; set; }

        public string Message { get; set; }

        public ActionResult OnGet()
        {
            String email = HttpContext.Session.GetString("Email");
            if (email == null) return RedirectToPage("/Login");
            if (email != null)
            {
                Korisnik k = _dbKorisnici.AsQueryable<Korisnik>().Where(x => x.Email == email).FirstOrDefault();
                if (k.Tip == 1)
                    return RedirectToPage("/Login");
                else { Message = "Menadzer"; }
            }
            return Page();
        }
        public async Task<IActionResult> OnPostAsync()
        {
            if (HttpContext.Session.GetString("Email") == null)
                return RedirectToPage("/Index");
            Korisnik kor = await _dbKorisnici.Find(kor => kor.Email == HttpContext.Session.GetString("Email")).FirstOrDefaultAsync();

            List<Kabina> noveKabine = new List<Kabina>();
            foreach (string kabina in kabine)
            {
                Kabina novaKabina = new Kabina();
                string labela = kabina.Substring(0, kabina.LastIndexOf('|'));
                int krevetnost;
                bool uspesno = Int32.TryParse(kabina.Substring(kabina.LastIndexOf('|') + 1), out krevetnost);
                if (!uspesno) return RedirectToPage("/Error");
                novaKabina.BrojMesta = krevetnost;
                novaKabina.BrojKabine= labela;
                noveKabine.Add(novaKabina);
            }

            int validImageCount = 0;
            if (!string.IsNullOrEmpty(slika1))
                validImageCount++;
            if (!string.IsNullOrEmpty(slika2))
                validImageCount++;
            if (!string.IsNullOrEmpty(slika3))
                validImageCount++;

            if (validImageCount != 3 || string.IsNullOrEmpty(glavnaSlika) || !validNovaKabina())
                return RedirectToPage();

            string folderName = System.Guid.NewGuid().ToString();
            string fileName = "";
            try
            {

                fileName = saveBase64AsImage(slika1, folderName);
                noviKruzer.Slika1 = "images/" + folderName + "/" + fileName;

                fileName = saveBase64AsImage(slika2, folderName);
                noviKruzer.Slika2 = "images/" + folderName + "/" + fileName;

                fileName = saveBase64AsImage(slika3, folderName);
                noviKruzer.Slika3 = "images/" + folderName + "/" + fileName;

                fileName = saveBase64AsImage(glavnaSlika, folderName);
                noviKruzer.GlavnaSlika = "images/" + folderName + "/" + fileName;
            }

            catch (FormatException fe)
            {
                RedirectToPage("/Error?errorCode=" + fe);
            }
            //    var count = noviKruzer.Gradovi.Count;
            //List<string> imena=new List<string>();
            //foreach(Grad g in noviKruzer.Gradovi)
            //{
            //    imena.Add(g.Naziv);//samo prihvata poslednjeg
            //}
            //var c1 = noviKruzer.Drzave.Count;
            //var siti1 = noviKruzer.Gradovi.Count;
            //var x = noviKruzer.Drzave[0].Naziv;//OVO RADI BREEEEE
            //var y = noviKruzer.Gradovi[0].Naziv;

            string[] stateArray = states.Split(',');
            string[] cityArray = cities.Split(',');

            List<Grad> g1 = new List<Grad>();
            List<Drzava> s1 = new List<Drzava>();
            foreach (string city in cityArray)
            {
                g1.Add(new Grad { Naziv = city, Slika = "/images/grad.jpg", Opis = "Veoma lep grad!" });
                Console.WriteLine(city);
            }
            foreach (string state in stateArray)
            {
                s1.Add(new Drzava { Naziv = state, Opis = "Veoma lepa drzava!" });
                Console.WriteLine(state);
            }
            noviKruzer.Gradovi = g1;
            noviKruzer.Drzave = s1;
            
            await _dbKruzeri.InsertOneAsync(noviKruzer);
            foreach (Kabina s in noveKabine)
            {
                s.Kruzer = new MongoDBRef("kruzeri", noviKruzer.Id);
            }


            //await _dbKabine.InsertManyAsync(noveKabine);//After many hours trying to figure out the issue i have found that the C# Drivers 2.11.0 have an issue. Depending on what other third party libraries have been used before using the driver they will just throw timeouts exception. Reverting the Driver to 2.10.4 fix the issue. So it’s something that have been added between both version. Right now i have found that if you open a MongoClient at the begining of the application (first line in the program.cs) it will work even after calling the third party dll’s that cause mongo to fail. If instead you call any of these third party dll then try to make any call to mongo you will always have timeout error. I have more details on the operation i was doing over at stackoverflow

            if (noveKabine != null && noveKabine.Count > 0)
            {
                await _dbKabine.InsertManyAsync(noveKabine);
            }
            else
            {
                Console.WriteLine("No documents to insert.");
            }
            var update = Builders<Kruzer>.Update.PushEach(Kruzer => Kruzer.Kabine, noveKabine.Select(kabina => new MongoDBRef("kabine", kabina.Id)));
            await _dbKruzeri.UpdateOneAsync(kruzer => kruzer.Id == noviKruzer.Id, update);

            var filter = Builders<Korisnik>.Filter.Eq(kori => kori.Id, kor.Id);
            var up = Builders<Korisnik>.Update.Set("Kruzer", new MongoDBRef("kruzeri", noviKruzer.Id));
            

        
            foreach(Grad g in noviKruzer.Gradovi)
            { 
            if (_dbGradovi.Find(grad => grad.Naziv==g.Naziv).FirstOrDefault() == null)
            {
                    _dbGradovi.InsertOne(new Grad { Naziv = g.Naziv, Slika = "/images/grad.jpg", Opis = g.Opis });

                }
            }
            foreach(Drzava d in noviKruzer.Drzave)
            {
                if (_dbDrzave.Find(drzava => drzava.Naziv == d.Naziv).FirstOrDefault() == null)
                {
                    _dbDrzave.InsertOne(new Drzava { Naziv = d.Naziv,  Opis = d.Opis });

                }
            }
        
                await _dbKorisnici.UpdateOneAsync(filter, up);
            return RedirectToPage("/Index");


        }

        public string saveBase64AsImage(string img, string folderName)
        {
            img = img.Substring(img.IndexOf(',') + 1);
            var imgConverted = Convert.FromBase64String(img);

            string imgName = System.Guid.NewGuid().ToString();

            var file = Path.Combine(_environment.ContentRootPath, "wwwroot/images/" + folderName + "/" + imgName + ".jpg");
            Directory.CreateDirectory(Path.GetDirectoryName(file));


            using (var fileStream = new FileStream(file, FileMode.Create))
            {
                fileStream.Write(imgConverted, 0, imgConverted.Length);
                fileStream.Flush();

            }
            return imgName + ".jpg";
        }

        public bool validNovaKabina()
        {
         
            if (string.IsNullOrEmpty(noviKruzer.Opis) || (string.IsNullOrWhiteSpace(noviKruzer.Opis)))
                return false;
            if (string.IsNullOrEmpty(noviKruzer.Naziv) || (string.IsNullOrWhiteSpace(noviKruzer.Naziv)))
                return false;
            if (string.IsNullOrEmpty(noviKruzer.KontaktTelefon) || (string.IsNullOrWhiteSpace(noviKruzer.KontaktTelefon)))
                return false;
            if (noviKruzer.BrojZvezdica < 1 || noviKruzer.BrojZvezdica > 6)//na netu pise da kruzeri imaju max 6 zvezdica
                return false;
            

            return true;

        }

    }
}

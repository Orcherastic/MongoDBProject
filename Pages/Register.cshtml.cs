using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MongoDB.Driver;
using PROJEKAT_MONGODB.Model;

namespace PROJEKAT_MONGODB.Pages
{
    public class RegisterModel : PageModel
    {
        [BindProperty]
        public Korisnik NoviKorisnik { get; set; }
        [BindProperty]
        public string ConfrimPassword { get; set; }
        public string ErrorMessage { get; set; }
        public string Message { get; set; }
        public void OnGet()
        {
        }
        public IActionResult OnPostRegister()
        {
            var client = new MongoClient("mongodb://localhost/?safe=true");
            var db = client.GetDatabase("SEVENSEAS");
            var collection = db.GetCollection<Korisnik>("korisnici");

            Korisnik k = collection.AsQueryable<Korisnik>().Where(x => x.Email == NoviKorisnik.Email).FirstOrDefault();
            if (!NoviKorisnik.Sifra.Equals(ConfrimPassword))
            {
                ErrorMessage = "Sifre se ne podudaraju!";
                return Page();
            }
            if (k != null)
            {
                ErrorMessage = "Vec postoji korisnik sa ovom email adresom!";
                return Page();
            }
            NoviKorisnik.Tip = 0;
            collection.InsertOne(NoviKorisnik);
            HttpContext.Session.SetString("Email", NoviKorisnik.Email);
            return RedirectToPage("/AddKruzer");
            //return new JsonResult(NoviKorisnik);
        }
    }
}

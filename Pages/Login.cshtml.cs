using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MongoDB.Driver;
using PROJEKAT_MONGODB.Model;
using Microsoft.AspNetCore.Http;
namespace PROJEKAT_MONGODB.Pages
{
    public class LoginModel : PageModel
    {
        [BindProperty]
        public string email { get; set; }
        [BindProperty]
        public string sifra { get; set; }
        public string ErrorMessage { get; set; }
        public string Message { get; set; }
        public void OnGet()
        {
        }

        public IActionResult OnPostLogin()
        {
            var client = new MongoClient("mongodb://localhost/?safe=true");
            var db = client.GetDatabase("SEVENSEAS");
            var collection = db.GetCollection<Korisnik>("korisnici");

            /*var filter = Builders<BsonDocument>.Filter.And(
                Builders<BsonDocument>.Filter.Eq("email", email),
                Builders<BsonDocument>.Filter.Eq("sifra", sifra)
            );*/

            Korisnik k = collection.AsQueryable<Korisnik>().Where(x => x.Email == email && x.Sifra == sifra).FirstOrDefault();

            if (k != null)
            {
                HttpContext.Session.SetString("Email", email);
                return RedirectToPage("/Index");
            }
            ErrorMessage = "Invalid email address or password!";
            return Page();
        }
    }
}

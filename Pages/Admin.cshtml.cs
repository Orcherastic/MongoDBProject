using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Http;
using PROJEKAT_MONGODB.Model;
using MongoDB.Driver;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace PROJEKAT_MONGODB.Pages
{
    public class AdminModel : PageModel
    {
        private readonly IMongoCollection<Kruzer> kr;
        private readonly IMongoCollection<Ponuda> p;
        private readonly IMongoCollection<Korisnik> ko;
        private readonly IMongoCollection<Rezervacija> r;
        private readonly IMongoCollection<Kabina> ka;
        [BindProperty]
        public List<Kruzer> kruzeri { get; set; }
        [BindProperty]
        public List<Korisnik> korisnici { get; set; }
        [BindProperty]
        public List<Rezervacija> rezervacije { get; set; }
        //[BindProperty]
        //public List<Kruzer> kruzeriMenadzera { get; set; }
        [BindProperty]
        public List<Ponuda> ponudeRezervacija { get; set; }
        [BindProperty]
        public List<Kruzer> kruzeriRezervacija { get; set; }
        [BindProperty]
        public List<Korisnik> menadzeriRezervacija { get; set; }
        [BindProperty]
        public List<Korisnik> menadzeri { get; set; }
        [BindProperty]
        public List<Ponuda> ponude { get; set; }
        //[BindProperty]
        //public List<Kruzer> kruzeriPonuda { get; set; }
        public string Message { get; set; }

        public AdminModel(/*IDatabaseSettings settings*/)
        {
            var client = new MongoClient("mongodb://localhost/?safe=true");
            var database = client.GetDatabase("SEVENSEAS");
            kr = database.GetCollection<Kruzer>("kruzeri");
            ka = database.GetCollection<Kabina>("kabine");
            ko = database.GetCollection<Korisnik>("korisnici");
            p = database.GetCollection<Ponuda>("ponude");
            r = database.GetCollection<Rezervacija>("rezervacije");
            //kruzeriMenadzera = new List<Kruzer>();
            menadzeriRezervacija = new List<Korisnik>();
            ponudeRezervacija = new List<Ponuda>();
            kruzeriRezervacija = new List<Kruzer>();
            menadzeri = new List<Korisnik>();
            ponude = new List<Ponuda>();
            //kruzeriPonuda = new List<Kruzer>();
        }

        public void OnGet()
        {
            String email = HttpContext.Session.GetString("Email");
            if (email != null)
            {
                Korisnik korisnik = ko.AsQueryable<Korisnik>().Where(x => x.Email == email).FirstOrDefault();
                if (korisnik.Tip == 0)
                    Message = "Menadzer";
                else Message = "Admin";
            }


            kruzeri = kr.Find(x => true).ToList();
            rezervacije = r.Find(x => true).ToList();
            korisnici = ko.Find(x => x.Tip == 0).ToList();  
            ponude = p.Find(x => true).ToList();
            foreach (Rezervacija rez in rezervacije)
            {
                ponudeRezervacija.Add(p.Find(x => x.Id.Equals(new ObjectId(rez.Ponuda.Id.ToString()))).FirstOrDefault());
                kruzeriRezervacija.Add(kr.Find(x => x.Id.Equals( new ObjectId(rez.Kruzer.Id.ToString()))).FirstOrDefault());
            }
            foreach (Kruzer kruzer in kruzeriRezervacija)
            {
                menadzeriRezervacija.Add(ko.Find(x => x.Kruzer.Id.Equals(kruzer.Id)).FirstOrDefault());
            }
            foreach (Kruzer kruzer in kruzeri)
            {
                //menadzeri.Add(k.Find(x=>x.Hotel.Id.Equals(hot.Id)).FirstOrDefault());
                //string s = k.AsQueryable<Korisnik>().Select(x=>x.Hotel.Id.AsString).FirstOrDefault();
                Korisnik m = ko.AsQueryable<Korisnik>().Where(x => x.Kruzer.Id == kruzer.Id).FirstOrDefault();
                menadzeri.Add(m);

            }
            //foreach (Korisnik kor in menadzeri)
            //{
            //    kruzeriMenadzera.Add(kr.Find(x => x.Id.Equals(new ObjectId(kor.Kruzer.Id.ToString()))).FirstOrDefault());
            //}
            //foreach (Korisnik kor in menadzeri)
            //{
            //    if(kor==null)
            //    {
            //        continue;
            //    }
            //    var kruzer = kr.Find(x => x.Id.Equals(new ObjectId(kor.Kruzer.Id.ToString()))).FirstOrDefault();
            //    if (kruzer != null)
            //    {
            //        kruzeriMenadzera.Add(kruzer);
            //    }
            //}

        }

        public IActionResult OnPostObrisiRez(string id)
        {
            List<Kabina> kabine = ka.Find(x => true).ToList();
            var pull = Builders<Kabina>.Update.PullFilter(x => x.Rezervacije, Builders<MongoDBRef>.Filter.Where(q => q.Id.Equals(new ObjectId(id))));
            foreach (Kabina kabina in kabine)
            {
                var filter = Builders<Kabina>.Filter.Eq("Id", kabina.Id);
                ka.UpdateOne(filter, pull);
            }
            r.DeleteOne<Rezervacija>(x => x.Id.Equals(new ObjectId(id)));
            return RedirectToPage();
        }

        public IActionResult OnPostStatusAktivno(string id)
        {
            var update = Builders<Rezervacija>.Update.Set("Status", "Aktivno");
            var filter = Builders<Rezervacija>.Filter.Eq("Id", new ObjectId(id));
            r.UpdateOne(filter, update);
            return RedirectToPage();
        }

        public IActionResult OnPostObrisiKruzer(string id)
        {
            Kruzer kruzer = kr.Find(x => x.Id.Equals(new ObjectId(id))).FirstOrDefault();
            //brisanje menadzera kruzera
            var filter1 = Builders<Korisnik>.Filter.Eq("Kruzer.Id", new ObjectId(id));
            ko.DeleteOne(filter1);
            //brisanje soba hotela
            foreach (MongoDBRef kabinaRef in kruzer.Kabine.ToList())
            {
                var filter2 = Builders<Kabina>.Filter.Eq("Id", kabinaRef.Id);
                ka.DeleteOne(filter2);
            }
            //brisanje ponuda vezanih za kruzer
            foreach (MongoDBRef ponudaRef in kruzer.Ponude.ToList())
            {
                var filter3 = Builders<Ponuda>.Filter.Eq("Id", ponudaRef.Id);
                p.DeleteOne(filter3);
            }
            //brisanje rezervacija vezanih za taj kruzer
            var filter = Builders<Rezervacija>.Filter.Eq("Kruzer.Id", new ObjectId(id));
            r.DeleteMany(filter);
            //brisanje kruzera
            kr.DeleteOne(x => x.Id.Equals(new ObjectId(id)));

            return RedirectToPage();
        }
    }
}

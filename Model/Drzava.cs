using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
namespace PROJEKAT_MONGODB.Model
{
    public class Drzava
    {
        public ObjectId Id { get; set; }
        public string Naziv { get; set; }
        public string Opis { get; set; }//da l ima smisla da stavimo opis drzave,posto imamo opis gradq, al mozemo sto da ne

        public List<Grad> Gradovi { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace YesSql.Tests.Models
{
    public class Email
    {
        public int Id { get; set; }
        public List<Attachement> Attachements { get; set; }
        public DateTime Date { get; set; }
    }
    public class Attachement
    {
        public Attachement(string Name)
        {
            this.Name = Name;
        }
        public string Name { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApplicationRobot.Events
{
    public class Evenement
    {
        public int Id { get; set; }
        public string EvenementName { get; set; }
        public DateTime Date { get; set; }
        public TimeSpan Heure { get; set; }
        public string Domaine { get; set; }
    }
}

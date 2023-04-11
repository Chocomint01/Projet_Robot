using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApplicationRobot.Models
{
    public static class AppSettings
    {
        public static UserModel CurrentUser { get; set; }
        public static Guid? LastSelectedDomaineId { get; set; }
    }
}

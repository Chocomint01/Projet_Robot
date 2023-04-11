using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Maps.MapControl.WPF;

namespace ApplicationRobot.Models
{
    public static class SharedData
    {
        public static List<Location> PolygonCoordinates { get; set; }
        public static Guid? SelectedDomaineId { get; set; }
        public delegate void PolygonDeletedEventHandler(object sender, EventArgs e);
        public static event PolygonDeletedEventHandler PolygonDeleted;
        public static event EventHandler PolygonSelected;

        public static void OnPolygonDeleted()
        {
            PolygonDeleted?.Invoke(null, EventArgs.Empty);
        }
        public static void RaisePolygonSelected()
        {
            PolygonSelected?.Invoke(null, EventArgs.Empty);
        }
    }

}

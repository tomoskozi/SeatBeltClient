using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeatBeltClient
{
    public class SeatBeltGET
{
        public bool secured { get; set; }
        public string id { get; set; }
    }

    public class SeatBeltPOST
    {
        public bool secured { get; set; }
    }
}

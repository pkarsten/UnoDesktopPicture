using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PiPic1.Models;
public class OutlookTaskFolder
    {
        //TODO: Needed? See TaskfolderResponse
        string Name { get;set;}

        string id { get; set; }
    }

    public class OutlookTask
    {
        string Subject { get; set; }
        string id { get; set; }
    }


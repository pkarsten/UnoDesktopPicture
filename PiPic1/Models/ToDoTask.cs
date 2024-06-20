using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PiPic1.Models;
public sealed class ToDoTask
    {
        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string Subject { get; set; }
        public string BodyText{ get; set; }
    }


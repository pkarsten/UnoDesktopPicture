using SQLite;
using System;

    public sealed class CalendarEvent
    {
        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public string Subject {get;set;}
        public DateTime StartDateTime {get;set;}
        public bool TodayEvent{ get; set; }
        public bool IsAllDay { get; set; }
        public bool IgnoreEvent { get; set; }
}

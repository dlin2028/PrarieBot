namespace PrarieBoi
{
    internal class ClassInfo
    {
        public string Id;
        public string Username;
        public string Password;
        public string CalendarId;

        public ClassInfo(string classId, string username, string password, string calendarId)
        {
            this.Id = classId;
            this.Username = username;
            this.Password = password;
            this.CalendarId = calendarId;
        }
    }
}
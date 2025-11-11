namespace Domain; 


    public class Mitarbeiter
    {
        public int id
        { get; set; }
        public string FirstName
        { get; set; }

        public string LastName
        { get; set; }
        public String BirthDate
        { get; set; }
        public bool IsActive
        { get; set; }


        public Mitarbeiter(int id, string firstName, string lastName, string birthDate, bool isActive)
        {
            this.id = id;

            this.FirstName = firstName;

            this.LastName = lastName;

            this.BirthDate = birthDate;

            this.IsActive = isActive;

        }

        public override string ToString()
        {
            return $"Mitarbeiter: ID= {id}, Vorname= {FirstName}, Nachname= {LastName}, Geburtsdatum= {BirthDate}, Aktiv= {IsActive}";
        }
    }

using Microsoft.AspNetCore.Identity;

namespace Security.Infrastructure.Security
{
    public class ApplicationUser : IdentityUser<Guid>
    {
        public string LastName { get; set; }
        public string FirstName { get; set; }
        public bool Active { get; set; }
        public string FullName { get { return FirstName + " " + LastName; } }
        public bool Staff { get; set; }

        public ApplicationUser(string username, string firstName, string lastName, bool active, bool staff) : base(username)
        {
            LastName = lastName;
            FirstName = firstName;
            Active = active;
            Staff = staff;
        }

        private ApplicationUser()
        {
            LastName = "";
            FirstName = "";
        }
    }

    public class ApplicationRole : IdentityRole<Guid> { }
}

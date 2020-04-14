using System.Collections.Generic;

namespace Shared
{
    public static class InMemoryUsers
    {
        public static IEnumerable<User> Users { get; set; } = new List<User>
        {
            new User
            {
                FirstName = "admin",
                LastName = "admin",
                Password = "admin",
                UserName = "admin"
            }
        };
    }
}

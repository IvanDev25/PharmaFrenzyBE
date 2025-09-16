using System.Collections.Generic;
using System.Numerics;

public class Manager
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int Age { get; set; }
    public string PhoneNumber { get; set; }
    public string Gender { get; set; }
    public int CityId { get; set; }
    public string Email { get; set; }

    //public Team Team { get; set; } // One-to-One
    //public ICollection<Player> Players { get; set; } // One-to-Many
}

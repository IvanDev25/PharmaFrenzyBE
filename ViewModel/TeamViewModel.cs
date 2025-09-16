namespace Api.ViewModel
{
    public class TeamViewModel
    {
        public int Id { get; set; }
        public string TeamName { get; set; }
        public int ManagerId { get; set; }
        public string ManagerName { get; set; }  // Manager information
        public int CategoryId { get; set; }
        public string CategoryName { get; set; }  // Category information
        public string Status { get; set; }
    }

}

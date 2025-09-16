namespace Api.DTOs.Account
{
    public class AdminPermission
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public bool PlayerManagement { get; set; }
        public bool AdminManagement { get; set; }
        public bool ManagerManagement { get; set; }
        public bool CategoryManagement { get; set; }
        public bool TeamManagement { get; set; }
        public System.DateTime AccessEndDate { get; set; }
    }
}

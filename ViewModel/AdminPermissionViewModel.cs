namespace Api.ViewModel
{
    public class AdminPermissionViewModel
    {
        public string UserId { get; set; }
        public int AdminPermissionId { get; set; }
        public string FirstName { get; set; }
        public string lastName { get; set; }
        public string Email { get; set; }
        public bool PlayerManagement { get; set; }
        public bool AdminManagement { get; set; }
        public bool ManagerManagement { get; set; }
        public bool CategoryManagement { get; set; }
        public bool TeamManagement { get; set; }
        public System.DateTime AccessEndDate { get; set; }
    }
}

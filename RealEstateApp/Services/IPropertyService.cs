using RealEstateApp.Models;

namespace RealEstateApp.Services
{
    public interface IPropertyService
    {
        List<Agent> GetAgents();
        List<Property> GetProperties();
        void SaveProperty(Property property);
        Task<LoginResult> LoginAsync(string username, string password);
        Task<LoginResult> TryAutoLoginAsync();
        void Logout();
    }
}

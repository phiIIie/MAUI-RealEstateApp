using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace RealEstateApp.Models
{
    public class Property : INotifyPropertyChanged
    {
        private string _aspect { get; set; }
        public string Aspect
        {
            get => _aspect;
            set
            {
                if (_aspect != value)
                {
                    _aspect = value;
                    OnPropertyChanged();
                }
            }
        }
        public Property()
        {
            Id = Guid.NewGuid().ToString();

            ImageUrls = new List<string>();
        }
        public Vendor Vendor { get; set; }

        public string Id { get; set; }
        public string Address { get; set; }
        public int? Price { get; set; }
        public string Description { get; set; }
        public int? Beds { get; set; }
        public int? Baths { get; set; }
        public int? Parking { get; set; }
        public int? LandSize { get; set; }
        public string AgentId { get; set; }
        public List<string> ImageUrls { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }

        public string MainImageUrl => ImageUrls?.FirstOrDefault() ?? GlobalSettings.Instance.NoImageUrl;

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string propertyName = "")
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

namespace ShipService.Models
{
    public class ShipService
    {
        public int Id { get; set; }
        public int ShipId { get; set; }
        public string Company { get; set; }
        public List<Container> Containers { get; set; } = new List<Container>();
    }
}
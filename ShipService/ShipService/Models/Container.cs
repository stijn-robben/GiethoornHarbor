namespace ShipService.Models
{
    public class Container
    {
        public int Id { get; set; }
        public string ProductName { get; set; }
        public ProductType ProductType { get; set; }
        public int Weight { get; set; }
    }
}

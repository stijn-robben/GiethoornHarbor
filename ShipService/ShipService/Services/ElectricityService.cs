namespace ShipService.Services
{
    public class ElectricityService
    {
        public void ProvideElectricity(int shipId, string company)
        {
            Console.WriteLine($"[ElectricityService] Providing electricity to ship {shipId} for company {company}...");
            // Simulate electricity logic
        }
    }
}
namespace ShipService.Services
{
    public class RefuelService
    {
        public void Refuel(int shipId, string company)
        {
            Console.WriteLine($"[RefuelService] Refueling ship {shipId} for company {company}...");
            // Simulate refueling logic
        }
    }
}
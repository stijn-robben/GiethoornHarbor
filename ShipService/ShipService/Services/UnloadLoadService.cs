using ShipService.Models;

namespace ShipService.Services
{
    public class UnloadLoadService
    {
        public void UnloadAndLoadContainers(List<Container> containers)
        {

            // Add PickupTime for each container
            var now = DateTime.UtcNow;
            var containersWithPickupTime = containers
                .Select((c, i) => new
                {
                    Container = c,
                    PickupTime = now.AddMinutes(-i * 10)
                })
                .ToList();

            // Unloading: Fresh/Livestock first, sorted by PickupTime ascending (oldest first)
            var unloadOptimized = containersWithPickupTime
                .OrderByDescending(x => x.Container.ProductType == ProductType.Fresh || x.Container.ProductType == ProductType.Livestock)
                .ThenBy(x => x.PickupTime)
                .Select(x => x.Container)
                .ToList();

            Console.WriteLine("[UnloadLoadService] Started unloading containers from ship... ");
            int unloadPos = 1;
            foreach (var c in unloadOptimized)
            {
                Console.WriteLine($"  {unloadPos++}. Unloading container {c.Id} ({c.ProductName}), Type={c.ProductType}");
            }
            Console.WriteLine("[UnloadLoadService] Unloading completed.");

            // Loading: Fresh/Livestock first, sorted by PickupTime descending (latest picked up = on top)
            var loadOptimized = containersWithPickupTime
                .OrderByDescending(x => x.Container.ProductType == ProductType.Fresh || x.Container.ProductType == ProductType.Livestock)
                .ThenByDescending(x => x.PickupTime)
                .Select(x => x.Container)
                .ToList();

            Console.WriteLine("[UnloadLoadService] Started loading containers on ship...");
            int loadPos = 1;
            foreach (var c in loadOptimized)
            {
                Console.WriteLine($"  {loadPos++}. Loading container {c.Id} ({c.ProductName}), Type={c.ProductType}");
            }
            Console.WriteLine("[UnloadLoadService] Loading completed.");
        }
    }
}
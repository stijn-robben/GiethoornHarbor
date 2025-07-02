using ShipService.Messaging;
using ShipService.Data;
using ShipService.Models;

namespace ShipService.Services
{
    public class ShipServiceManager
    {
        private readonly RabbitMqPublisher _eventPublisher;
        private readonly IServiceProvider _serviceProvider;
        private readonly RefuelService _refuelService;
        private readonly ElectricityService _electricityService;
        private readonly UnloadLoadService _unloadLoadService;
        private int _amount = 0;

        public ShipServiceManager(
            RabbitMqPublisher eventPublisher,
            IServiceProvider serviceProvider,
            RefuelService refuelService,
            ElectricityService electricityService,
            UnloadLoadService unloadLoadService)
        {
            _eventPublisher = eventPublisher;
            _serviceProvider = serviceProvider;
            _refuelService = refuelService;
            _electricityService = electricityService;
            _unloadLoadService = unloadLoadService;
        }

        public void Execute(int shipId, string company)
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ShipServiceContext>();

            var shipService = new Models.ShipService
            {
                ShipId = shipId,
                Company = company
            };

            // Fill the ship with random containers
            shipService.Containers = RandomizeContainersOnShip();

            db.ShipServices.Add(shipService);
            db.SaveChanges();
            Console.WriteLine($"[ExecuteShipService] Created new ShipService for ShipId={shipId}, Company={company}");

            // Execute all services
            _refuelService.Refuel(shipId, company);
            _electricityService.ProvideElectricity(shipId, company);
            _unloadLoadService.UnloadAndLoadContainers(shipService.Containers);

            // Calculate amount for invoice
            int baseAmount = 1000;
            int containerFee = shipService.Containers.Count * 20;
            _amount = baseAmount + containerFee;

            Console.WriteLine($"[ExecuteShipService] Calculated invoice amount: {baseAmount} (all services) + {containerFee} (containers) = {_amount}");

            foreach (var container in shipService.Containers)
            {
                Console.WriteLine($"[ExecuteShipService] Container: Id={container.Id}, Item={container.ProductName}, Type={container.ProductType}, Weight={container.Weight}");
            }

            // Publish invoice event
            PublishInvoiceEvent(company, _amount, DateTime.UtcNow);
        }

        private List<Container> RandomizeContainersOnShip()
        {
            var rnd = new Random();
            int count = rnd.Next(20, 101); // 20 to 100 containers
            var containers = new List<Container>();
            var productTypes = Enum.GetValues(typeof(ProductType)).Cast<ProductType>().ToArray();

            var productNamesByType = new Dictionary<ProductType, string[]>
            {
                { ProductType.Normal,   new[] { "Teddybears", "Shoes", "Books", "Electronics", "Furniture", "Clothing", "Bicycles" } },
                { ProductType.Fresh,    new[] { "Lettuce", "Tomatoes", "Apples", "Bananas", "Salmon", "Milk", "Cheese" } },
                { ProductType.Livestock,new[] { "Chicken", "Cows", "Sheep", "Pigs", "Goats", "Horses" } },
                { ProductType.Fragile,  new[] { "Glassware", "Porcelain", "Laptops", "Smartphones", "Mirrors", "Lightbulbs" } },
                { ProductType.Explosive,new[] { "C4", "Dynamite", "Fireworks", "Ammunition", "Propane Tanks" } }
            };

            for (int i = 0; i < count; i++)
            {
                var type = productTypes[rnd.Next(productTypes.Length)];
                var names = productNamesByType[type];
                var productName = names[rnd.Next(names.Length)];

                containers.Add(new Container
                {
                    ProductName = productName,
                    ProductType = type,
                    Weight = rnd.Next(1000, 30001) // 1,000 to 30,000 kg
                });
            }
            return containers;
        }

        private void PublishInvoiceEvent(string shippingCompanyName, int amount, DateTime generatedAt)
        {
            var invoiceEvent = new
            {
                EventType = "ShipServiceInvoice",
                ShipmentCompany = shippingCompanyName,
                Amount = amount,
                GeneratedAt = generatedAt
            };

            try
            {
                _eventPublisher.Publish(invoiceEvent);
                Console.WriteLine($"[ExecuteShipService] Published ShipServiceInvoice event for {shippingCompanyName} with amount {amount}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ExecuteShipService] Error publishing event: {ex}");
                throw;
            }
        }
    }
}

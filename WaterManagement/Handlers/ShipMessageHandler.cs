using WaterManagement.Dto;
using WaterManagement.Services;

namespace WaterManagement.Handlers
{
    public class ShipMessageHandler
    {
        private readonly WaterQualityService _waterQualityService;

        public ShipMessageHandler(WaterQualityService waterQualityService)
        {
            _waterQualityService = waterQualityService;
        }

        // Deze method call je wanneer je een "ship arrived" message krijgt van de bus
        public async Task HandleShipArrived(ShipArrivedMessage message)
        {
            await _waterQualityService.HandleShipArrivedAsync();
        }

        // Deze method call je wanneer je een "ship departed" message krijgt van de bus
        public async Task HandleShipDeparted(ShipDepartedMessage message)
        {
            await _waterQualityService.HandleShipDepartedAsync();
        }
    }
}

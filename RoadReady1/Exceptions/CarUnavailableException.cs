namespace RoadReady1.Exceptions
{
    public class CarUnavailableException : BadRequestException
    {
        public CarUnavailableException(int carId)
            : base($"Car {carId} is not available for the requested period.") { }
    }
}

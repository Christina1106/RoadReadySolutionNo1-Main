using System;
namespace RoadReady1.Exceptions
{
    public class UnauthorizedException : Exception
    {
        public UnauthorizedException(string message = "Invalid credentials.")
            : base(message) { }
    }
}

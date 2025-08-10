using System;
namespace RoadReady1.Exceptions
{
    public class InvalidTokenException : Exception
    {
        public InvalidTokenException(string message = "Invalid or expired token.") : base(message) { }
    }
}

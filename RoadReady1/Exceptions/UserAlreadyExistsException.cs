using System;
namespace RoadReady1.Exceptions
{
    public class UserAlreadyExistsException : Exception
    {
        public UserAlreadyExistsException(string message = "A user with this email already exists.")
            : base(message) { }
    }
}

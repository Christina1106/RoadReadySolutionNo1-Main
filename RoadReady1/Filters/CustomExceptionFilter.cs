using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace RoadReady1.Filters
{
    public class CustomExceptionFilter : ExceptionFilterAttribute
    {
        public override void OnException(ExceptionContext context)
        {
            // Map your domain exceptions to proper codes/messages
            var (status, message) = context.Exception switch
            {
                RoadReady1.Exceptions.NotFoundException ex => (StatusCodes.Status404NotFound, ex.Message),
                RoadReady1.Exceptions.BadRequestException ex => (StatusCodes.Status400BadRequest, ex.Message),
                RoadReady1.Exceptions.UnauthorizedException ex => (StatusCodes.Status401Unauthorized, ex.Message),
                _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.")
            };

            context.Result = new ObjectResult(new { message })
            {
                StatusCode = status
            };
            context.ExceptionHandled = true;
        }
    }
}

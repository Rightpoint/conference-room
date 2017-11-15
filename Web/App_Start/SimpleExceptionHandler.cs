using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.ExceptionHandling;
using System.Web.Http.Filters;
using System.Web.Http.Results;
using log4net;
using Microsoft.ApplicationInsights;
using RightpointLabs.ConferenceRoom.Web.Attributes;
using RightpointLabs.ConferenceRoom.Web.Controllers;

namespace RightpointLabs.ConferenceRoom.Web
{
    internal class SimpleExceptionHandler : IExceptionHandler
    {
        private static readonly ILog __log = LogManager.GetLogger(typeof(SimpleExceptionHandler));

        public virtual Task HandleAsync(ExceptionHandlerContext context, CancellationToken cancellationToken)
        {
            var log = __log;

            try
            {
                if (context.Exception != null)
                {
                    try
                    {
                        new TelemetryClient().TrackException(context.Exception);
                    }
                    catch
                    {
                        // ignore errors reporting errors
                    }
                }

                var baseController = context.ExceptionContext?.ActionContext?.ControllerContext?.Controller as BaseController;

                if (baseController != null)
                {
                    log = baseController.Log;
                }

                if (context.Exception != null &&
                    ErrorHandlerAttribute.IsAccessDeniedException(context.Exception))
                {
                    context.Result = new UnauthorizedResult(new AuthenticationHeaderValue[0], context.Request);
                }
            }
            catch (Exception ex)
            {
                //We had an error handling the error...
                log.ErrorFormat("Time: {0}, Message: {1}", DateTime.Now, ex);
            }
            return Task.FromResult(0);
        }

        public virtual bool ShouldHandle(ExceptionHandlerContext context)
        {
            return true;
        }

        public virtual void Handle(ExceptionHandlerContext context)
        {
        }
    }
}
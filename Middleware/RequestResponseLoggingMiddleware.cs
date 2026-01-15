using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace HospIntel.EmployeeService.Middleware
{
    public class RequestResponseLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestResponseLoggingMiddleware> _logger;
        private readonly string _serviceName;

        public RequestResponseLoggingMiddleware(RequestDelegate next, ILogger<RequestResponseLoggingMiddleware> logger)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serviceName = Assembly.GetEntryAssembly()?.GetName().Name ?? "UnknownService";
        }

        public async Task Invoke(HttpContext context)
        {
            var sw = Stopwatch.StartNew();

            try
            {
                context.Request.EnableBuffering();
            }
            catch
            {
                // Ignore if buffering cannot be enabled
            }

            string requestBody = string.Empty;
            try
            {
                if (context.Request.ContentLength > 0 && context.Request.Body.CanRead)
                {
                    using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
                    requestBody = await reader.ReadToEndAsync().ConfigureAwait(false);
                    context.Request.Body.Position = 0;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to read request body for {Service} {Method} {Path}", _serviceName, context.Request.Method, context.Request.Path);
            }

            _logger.LogInformation("Incoming Request {Service} {Method} {Path} Query:{Query} Body:{Body}", _serviceName, context.Request.Method, context.Request.Path, context.Request.QueryString, string.IsNullOrWhiteSpace(requestBody) ? "<empty>" : requestBody);
            // when reading request/response bodies:
            string Truncate(string s, int max = 4096) => s?.Length > max ? s.Substring(0, max) + "...(truncated)" : s;
            _logger.LogInformation("Incoming Request Body: {Body}", Truncate(requestBody));

            var originalBody = context.Response.Body;
            await using var memStream = new MemoryStream();
            context.Response.Body = memStream;

            try
            {
                await _next(context).ConfigureAwait(false);

                context.Response.Body.Seek(0, SeekOrigin.Begin);
                string responseBody = string.Empty;
                try
                {
                    using var reader = new StreamReader(context.Response.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
                    responseBody = await reader.ReadToEndAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to read response body for {Service} {Method} {Path}", _serviceName, context.Request.Method, context.Request.Path);
                }

                context.Response.Body.Seek(0, SeekOrigin.Begin);
                await memStream.CopyToAsync(originalBody).ConfigureAwait(false);
                sw.Stop();

                _logger.LogInformation("Outgoing Response {Service} {Method} {Path} Status:{StatusCode} ElapsedMs:{Elapsed} ResponseBody:{Body}", _serviceName, context.Request.Method, context.Request.Path, context.Response.StatusCode, sw.ElapsedMilliseconds, string.IsNullOrWhiteSpace(responseBody) ? "<empty>" : responseBody);
            }
            catch (Exception ex)
            {
                sw.Stop();
                var trace = new StackTrace(ex, true);
                var frame = trace.GetFrames()?.FirstOrDefault(f => f.GetFileLineNumber() > 0) ?? trace.GetFrame(0);
                var file = frame?.GetFileName();
                var line = frame?.GetFileLineNumber();
                var proc = ex.Data.Contains("Procedure") ? ex.Data["Procedure"] : (ex.InnerException?.Data.Contains("Procedure") == true ? ex.InnerException.Data["Procedure"] : null);

                _logger.LogError(ex, "Unhandled exception in {Service} {Method} {Path} Proc:{Proc} Exception:{ExName} at {File}:{Line} ElapsedMs:{Elapsed}", _serviceName, context.Request.Method, context.Request.Path, proc ?? "<unknown>", ex.GetType().FullName, file ?? "<unknown>", line ?? 0, sw.ElapsedMilliseconds);

                throw;
            }
            finally
            {
                context.Response.Body = originalBody;
            }
        }
    }
}

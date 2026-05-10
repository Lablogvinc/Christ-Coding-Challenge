using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;

var processor = new ArticleProcessor();
var cts = new CancellationTokenSource();

Console.WriteLine("Starting periodic updates. Press any key to stop...");

var updateTask = processor.RunPeriodicUpdatesAsync(cts.Token);
var httpTask = RunHttpListenerAsync(processor, 5000, cts.Token);

Console.ReadKey(true); // Wait for key press

cts.Cancel();

await Task.WhenAll(updateTask, httpTask);

Console.WriteLine("Application stopped.");

static async Task RunHttpListenerAsync(ArticleProcessor processor, int port, CancellationToken token)
{
    var listener = new HttpListener();
    listener.Prefixes.Add($"http://*:{port}/");

    try
    {
        listener.Start();
        Console.WriteLine($"HTTP listener started on port {port}.");

        while (!token.IsCancellationRequested)
        {
            HttpListenerContext context;
            try
            {
                context = await listener.GetContextAsync().WaitAsync(token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (HttpListenerException)
            {
                break;
            }

            _ = Task.Run(async () =>
            {
                try
                {
                    string responseText;
                    string contentType = "text/plain";
                    if (context.Request.Url.AbsolutePath == "/api/aggregates")
                    {
                        var language = context.Request.QueryString["language"];
                        var aggregates = await processor.GetAggregatesAsync(language);
                        responseText = JsonSerializer.Serialize(aggregates);
                        contentType = "application/json";
                    }
                    else
                    {
                        responseText = "OK";
                    }

                    var responseBytes = Encoding.UTF8.GetBytes(responseText);
                    context.Response.StatusCode = 200;
                    context.Response.ContentType = contentType;
                    context.Response.ContentLength64 = responseBytes.Length;
                    await context.Response.OutputStream.WriteAsync(responseBytes, token);
                }
                catch (Exception ex)
                {
                    var errorResponse = JsonSerializer.Serialize(new { error = ex.Message });
                    var responseBytes = Encoding.UTF8.GetBytes(errorResponse);
                    context.Response.StatusCode = 500;
                    context.Response.ContentType = "application/json";
                    context.Response.ContentLength64 = responseBytes.Length;
                    await context.Response.OutputStream.WriteAsync(responseBytes, token);
                }
                finally
                {
                    context.Response.OutputStream.Close();
                }
            }, token);
        }
    }
    finally
    {
        if (listener.IsListening)
            listener.Stop();

        listener.Close();
        Console.WriteLine($"HTTP listener on port {port} stopped.");
    }
}

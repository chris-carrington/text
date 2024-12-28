using System.Net;
using System.Web;
using System.Text;
using System.Text.Json;


class Program
{
    public static async Task Main (string[] args)
    {
        string apiToken, phone;

        try
        {
            (apiToken, phone) = GetEnvironmentVariables();
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception.Message);
            return;
        }

        var listener = StartServer();

        while (true)
        {
            var (request, response) = await GetContextAsync(listener);

            try
            {
                ValidateRequest(request);
                await OnSuccessAsync(response, await SendTextAsync(apiToken, phone));
            }
            catch (Exception exception)
            {
                OnError(response, exception.Message);
                continue;
            }

        }
    }


    private static async Task<HttpResponseMessage> SendTextAsync (string apiToken, string phone)
    {
        // https://simpletexting.com/api/docs/#tag/Messages
        // curl --request POST \
            // --url 'https://app2.simpletexting.com/v1/send?token=YOUR_API_TOKEN&phone=SOME_STRING_VALUE&message=SOME_STRING_VALUE' \
            // --header 'accept: application/json' \
            // --header 'content-type: application/x-www-form-urlencoded'
        var url = $"https://app2.simpletexting.com/v1/send?token={ apiToken }&phone={ HttpUtility.UrlEncode(phone) }&message={ HttpUtility.UrlEncode("Greetings world!") }";
        var request = new HttpRequestMessage(HttpMethod.Post, url); // init a post request
        request.Headers.Add("accept", "application/json"); // set accept header to request
        request.Content = new StringContent("", Encoding.UTF8, "application/x-www-form-urlencoded"); // set request body to an empty string AND set content-type header to application/x-www-form-urlencoded

        using HttpClient httpClient = new(); // provides a class for sending HTTP requests and receiving HTTP responses
        return await httpClient.SendAsync(request);
    }


    private static async Task OnSuccessAsync (HttpListenerResponse response, HttpResponseMessage textResponseAsHttpResponse)
    {
        var textResponseAsString = await textResponseAsHttpResponse.Content.ReadAsStringAsync(); // http response to string
        var textResponseAsJSON = JsonSerializer.Deserialize<JsonElement>(textResponseAsString); // string to json
        var jsonResponse = new { success = true, simpleTextResponse = textResponseAsJSON }; // add { success: true } to response from Simple Text
        WriteResponse(response, JsonToByteArray(jsonResponse));
    }


    private static (string apiToken, string phone) GetEnvironmentVariables ()
    {
        DotNetEnv.Env.Load();

        var apiToken = Environment.GetEnvironmentVariable("SimpleTextAPIToken");
        var phone = Environment.GetEnvironmentVariable("SimpleTextPhoneNumber");

        if (string.IsNullOrEmpty(apiToken)) throw new Exception("Please define environment varialbe: SimpleTextAPIToken");
        if (string.IsNullOrEmpty(phone)) throw new Exception("Please define environment varialbe: SimpleTextPhoneNumber");

        return (apiToken, phone);
    }


    private static HttpListener StartServer ()
    {
        HttpListener listener = new();
        listener.Prefixes.Add("http://localhost:3000/");
        listener.Start();
        Console.WriteLine("Server is listening on http://localhost:3000/");

        return listener;
    }


    private static async Task<(HttpListenerRequest request, HttpListenerResponse response)> GetContextAsync (HttpListener listener)
    {
        HttpListenerContext context = await listener.GetContextAsync();
        return (request: context.Request, response: context.Response);
    }


    private static void ValidateRequest (HttpListenerRequest request)
    {
        if (request.HttpMethod != "GET") throw new Exception("Please call with a method of GET");
        if (request.Url == null || request.Url.AbsolutePath != "/send") throw new Exception("Please call the endpoint /send");
    }


    private static void OnError (HttpListenerResponse response, string message)
    {
        var jsonResponse = new { success = false, message };
        WriteResponse(response, JsonToByteArray(jsonResponse), (int)HttpStatusCode.BadRequest);
    }


    private static void WriteResponse (HttpListenerResponse response, byte[] buffer, int statusCode = 200)
    {
        response.StatusCode = statusCode;
        response.ContentLength64 = buffer.Length;
        response.ContentType = "application/json";

        using var output = response.OutputStream;
        output.Write(buffer, 0, buffer.Length); // writes data to the OutputStream
    }


    private static byte[] JsonToByteArray (object jsonResponse)
    {
        var jsonString = JsonSerializer.Serialize(jsonResponse); // convert json object to a string
        return Encoding.UTF8.GetBytes(jsonString); // convert string to byte array
    }
}

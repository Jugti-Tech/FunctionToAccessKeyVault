using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Web;

namespace FunctionForKeyVault
{
    public class KeyVaultFunction
    {
        private readonly ILogger<KeyVaultFunction> _logger;

        public KeyVaultFunction(ILogger<KeyVaultFunction> logger)
        {
            _logger = logger;
        }

        [Function("Function1")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");
            string kvUri = "https://jugtikeyvault.vault.azure.net";
            SecretClient client = new SecretClient(new Uri(kvUri), new DefaultAzureCredential());

            // retrieve the secret names from the query string

            string query = req.Url.Query;
            var queryItems = HttpUtility.ParseQueryString(query);
            string secretNamesItems = queryItems["SecretNames"] ?? string.Empty;
            string[] secretNames = secretNamesItems.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            if (secretNames.Length == 0)
            {
                return await CreateErrorResponse(req, "Error retrieving secret: No secret names provided.");
            }
            var secrets = new Dictionary<string, string>();
            foreach (string secretName in secretNames)
            {
                try
                {
                    KeyVaultSecret secret = await client.GetSecretAsync(secretName);
                    if (secret?.Value != null)
                    {
                        _logger.LogInformation($"Successfully retrieved secret: {secret.Value}");
                        secrets.Add(secretName, secret.Value);
                    }
                    else
                    {
                        _logger.LogError($"Secret {secretName} is null or empty.");
                        secrets.Add(secretName, "null or empty");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Exception occurred: {secretName}:{ex.Message}");
                    return await CreateErrorResponse(req, $"Error retrieving secret: {ex.Message}");
                }
            }
            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");
            await response.WriteAsJsonAsync(secrets);
            return response;

        }


        private static async Task<HttpResponseData> CreateErrorResponse(HttpRequestData req, string message)
        {
            var response = req.CreateResponse(HttpStatusCode.InternalServerError);

            response.Headers.Add("Content-Type", "text/plain; charset=utf-8");

            await response.WriteStringAsync(message);

            return response;
        }
    }
}

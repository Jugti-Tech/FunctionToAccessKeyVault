using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;

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
            var client = new SecretClient(new Uri(kvUri), new DefaultAzureCredential());

            try
            {
                KeyVaultSecret secret = await client.GetSecretAsync("dev-townmilk-milkman-mongoDBAppId");

                if (secret?.Value != null)
                {
                    _logger.LogInformation($"Successfully retrieved secret: {secret.Value}");
                    var response = req.CreateResponse(HttpStatusCode.OK);
                    response.Headers.Add("Content-Type", "text/plain; charset=utf-utf-8");

                    // Optionally, output the secret value; be careful with sensitive information
                    // response.WriteString($"Secret value: {secret.Value}");

                    await response.WriteStringAsync("Secret retrieved successfully.");
                    return response;
                }

                else
                {
                    _logger.LogError("Secret is null or empty.");
                    return CreateErrorResponse(req, "Error retrieving secret: Secret is null or empty.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception occurred: {ex.Message}");
                return CreateErrorResponse(req, $"Error retrieving secret: {ex.Message}");
            }
        }


        private static HttpResponseData CreateErrorResponse(HttpRequestData req, string message)
        {
            var response = req.CreateResponse(HttpStatusCode.InternalServerError);

            response.Headers.Add("Content-Type", "text/plain; charset=utf-8");

            response.WriteStringAsync(message);

            return response;
        }
    }
}

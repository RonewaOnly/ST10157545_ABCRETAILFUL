using System.Text.Json;

namespace ABCRETAIL.WEB.Services.FunctionClients
{
    internal static class FunctionJsonOptions
    {
        public static readonly JsonSerializerOptions Default = new(JsonSerializerDefaults.Web);
    }
}

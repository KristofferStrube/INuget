using System.Net.Http.Json;
using System.Text.Encodings.Web;
using System.Text.Json.Serialization;
using System.Web;

namespace KristofferStrube.INuget.Client.Services;

public class NugetClient(HttpClient httpClient)
{
    public async Task<List<PackageVersion>> Versions(string package)
    {
        return (await httpClient.GetFromJsonAsync<List<PackageVersion>>($"https://kristoffer-strube.dk/API/nuget/versions/{HttpUtility.UrlEncode(package)}/"))!;
    }

    public async Task<List<DependencyResponse>> Dependencies(string package, string version)
    {
        return (await httpClient.GetFromJsonAsync<List<DependencyResponse>>($"https://kristoffer-strube.dk/API/nuget/dependencies/{HttpUtility.UrlEncode(package)}/{HttpUtility.UrlEncode(version)}"))!;
    }

    public async Task<Stream> DLL(string package, string version)
    {
        return await httpClient.GetStreamAsync($"https://kristoffer-strube.dk/API/nuget/dll/{HttpUtility.UrlEncode(package)}/{HttpUtility.UrlEncode(version)}");
    }

    public class PackageVersion
    {
        [JsonPropertyName("version")]
        public required string Version { get; set; }

        [JsonPropertyName("published")]
        public DateTimeOffset Published { get; set; }
    }
    public class DependencyResponse
    {
        [JsonPropertyName("id")]
        public required string Id { get; set; }

        [JsonPropertyName("version")]
        public required string Version { get; set; }
    }
}
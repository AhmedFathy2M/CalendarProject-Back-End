using Newtonsoft.Json;

namespace Core.Dtos;

public class GoogleTokenResponse
{
    [JsonProperty("access_type")] public string AccessType { get; set; }

    [JsonProperty("expires_in")] public long ExpiresIn { get; set; }

    [JsonProperty("refresh_token")] public string RefreshToken { get; set; }

    [JsonProperty("scope")] public string Scope { get; set; }

    [JsonProperty("token_type")] public string TokenType { get; set; }
}
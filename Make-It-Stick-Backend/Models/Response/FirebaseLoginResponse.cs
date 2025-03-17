using Newtonsoft.Json;

public class FirebaseLoginResponse
{
    [JsonProperty("kind")]
    public string Kind { get; set; }

    [JsonProperty("localId")]
    public string LocalId { get; set; }

    [JsonProperty("email")]
    public string Email { get; set; }

    [JsonProperty("displayName")]
    public string DisplayName { get; set; }

    [JsonProperty("idToken")]
    public string IdToken { get; set; }

    [JsonProperty("registered")]
    public bool Registered { get; set; }

    [JsonProperty("refreshToken")]
    public string RefreshToken { get; set; }

    [JsonProperty("expiresIn")]
    public string ExpiresIn { get; set; }
}

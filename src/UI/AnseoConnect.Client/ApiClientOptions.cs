namespace AnseoConnect.Client;

public sealed class ApiClientOptions
{
    public Uri BaseAddress { get; set; } = new("https://localhost:5001/");
    public bool UseStubData { get; set; } = true;
}

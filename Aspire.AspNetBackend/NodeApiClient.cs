namespace Aspire.AspNetBackend;

public class NodeApiClient 
{ 
    private readonly HttpClient _client;

    public NodeApiClient(HttpClient client)    
    {
        _client = client;
    }

    public async Task<string> GetHello()
    {
        var response = await _client.GetAsync("/");
        return await response.Content.ReadAsStringAsync();
    }
}

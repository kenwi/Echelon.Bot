using System.Text.Json;

namespace Echelon.Bot.Interfaces;

public interface IHttpService
{
    Task<HttpResponseMessage> PostJsonAsync<T>(string endpoint, T data);
    string Endpoint { get; }
} 
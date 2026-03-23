using System.Net.Http.Json;

namespace Kairudev.Maui.Services;

public sealed class TaskApiClient
{
    private readonly HttpClient _http;

    public TaskApiClient(HttpClient http) => _http = http;

    public async Task<List<TaskDto>> GetAllAsync(string? search = null, string statusFilter = "OpenOnly")
    {
        var url = $"api/tasks?status={statusFilter}";
        if (!string.IsNullOrWhiteSpace(search))
            url += $"&search={Uri.EscapeDataString(search)}";
        return await _http.GetFromJsonAsync<List<TaskDto>>(url) ?? [];
    }

    public async Task<TaskDto?> GetByIdAsync(Guid id)
    {
        var response = await _http.GetAsync($"api/tasks/{id}");
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<TaskDto>();
    }

    public async Task<TaskDto?> AddAsync(string title, string? description = null, List<string>? tags = null)
    {
        var response = await _http.PostAsJsonAsync("api/tasks", new { Title = title, Description = description, Tags = tags ?? [] });
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<TaskDto>();
    }

    public async Task<TaskDto?> UpdateAsync(Guid id, string title, string? description = null, List<string>? tags = null)
    {
        var response = await _http.PutAsJsonAsync($"api/tasks/{id}", new { Title = title, Description = description, Tags = tags ?? [] });
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<TaskDto>();
    }

    public async Task<bool> CompleteAsync(Guid id)
    {
        var response = await _http.PutAsync($"api/tasks/{id}/complete", null);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var response = await _http.DeleteAsync($"api/tasks/{id}");
        return response.IsSuccessStatusCode;
    }
}

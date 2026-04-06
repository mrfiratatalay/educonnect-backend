using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

const string userId = "E51F36E8-FDAE-4639-BC1C-41DDFD7D8197";
const string fullName = "Phase Two User Two";
const string email = "phase2.user2.1775416556@ktu.edu.tr";
const string role = "Student";
const string issuer = "EduConnect";
const string audience = "EduConnectUsers";
const string secret = "EduConnect.SuperSecretKey.For.Jwt.Auth.2026";

var claims = new List<Claim>
{
    new(ClaimTypes.NameIdentifier, userId),
    new(ClaimTypes.Name, fullName),
    new(ClaimTypes.Email, email),
    new(ClaimTypes.Role, role),
};

var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

var token = new JwtSecurityToken(
    issuer: issuer,
    audience: audience,
    claims: claims,
    expires: DateTime.UtcNow.AddMinutes(60),
    signingCredentials: credentials);

var accessToken = new JwtSecurityTokenHandler().WriteToken(token);

using var httpClient = new HttpClient();
httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

var targetUrl = args.Length > 0
    ? args[0]
    : "http://localhost:5160/api/posts/trending";
var method = args.Length > 1
    ? args[1].ToUpperInvariant()
    : "GET";

using var request = new HttpRequestMessage(new HttpMethod(method), targetUrl);

if (method is "POST" or "PUT" or "PATCH")
{
    request.Content = JsonContent.Create(new { });
}

var response = await httpClient.SendAsync(request);
Console.WriteLine($"Status: {(int)response.StatusCode}");
Console.WriteLine(await response.Content.ReadAsStringAsync());

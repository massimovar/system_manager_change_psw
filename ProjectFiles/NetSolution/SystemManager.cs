#region Using directives
using System;
using UAManagedCore;
using FTOptix.NetLogic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Net.Http.Headers;
using FTOptix.System;
using FTOptix.UI;
#endregion

public class SystemManager : BaseNetLogic
{
    private HttpClient httpClient;
    private IUAVariable IP;
    private IUAVariable user;
    private IUAVariable password;

    private string AuthenticationToken;
    private string baseUrl;

    public override void Start()
    {
        baseUrl = string.Empty;
        IP = LogicObject.GetVariable("IP");
        user = LogicObject.GetVariable("user");
        password = LogicObject.GetVariable("password");
    }

    public override void Stop()
    {
        // Insert code to be executed when the user-defined logic is stopped
    }

    [ExportMethod]
    public void SystemManagerLogin(out bool loginOk)
    {
        SetEndpoint();
        loginOk = Login(user.Value.Value.ToString(), password.Value.Value.ToString());
    }

    [ExportMethod]
    public void SystemManagerChangePassword(string newPassword, out bool changePasswordOk)
    {
        changePasswordOk = ChangePassword(user.Value.Value.ToString(), newPassword);
    }

    private bool Login(string username, string password)
    {
        var requestBody = new StringContent(JsonSerializer.Serialize(new { username = username, password = password }), Encoding.UTF8, "application/json");
        requestBody.Headers.ContentType = new MediaTypeHeaderValue("application/json"); // Encoding.UTF8 does not like this charset
        return AuthenticateRequest(requestBody);
    }

    private bool ChangePassword(string username, string password)
    {
        var requestBody = new StringContent(JsonSerializer.Serialize(new { username = username, password = password }), Encoding.UTF8, "application/json");
        requestBody.Headers.ContentType = new MediaTypeHeaderValue("application/json"); // Encoding.UTF8 does not like this charset
        return ChangePasswordRequest(requestBody);
    }

    private void SetEndpoint()
    {
        baseUrl = $"https://{IP.Value.Value}";

        var httpClientHandler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true
        };
        httpClient = new HttpClient(httpClientHandler)
        {
            BaseAddress = new Uri(baseUrl),
            Timeout = TimeSpan.FromSeconds(5)
        };
    }

    private bool AuthenticateRequest(StringContent requestBody)
    {
        var response = httpClient.PostAsync("login", requestBody).Result;
        if (!response.IsSuccessStatusCode)
        {
            Log.Error("AuthenticateRequest", response.StatusCode.ToString());
            return false;
        }
        AuthenticationToken = response.Content.ReadAsStringAsync().Result;
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AuthenticationToken);
        return true;
    }
    private bool ChangePasswordRequest(StringContent requestBody)
    {
        var response = httpClient.PatchAsync("api/users/accounts", requestBody).Result;
        if (!response.IsSuccessStatusCode)
        {
            Log.Error("ChangePasswordRequest", response.StatusCode.ToString());
            return false;
        }
        return true;
    }
}

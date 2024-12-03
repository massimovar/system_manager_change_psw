#region Using directives
using System;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.UI;
using FTOptix.NativeUI;
using FTOptix.HMIProject;
using FTOptix.Retentivity;
using FTOptix.CoreBase;
using FTOptix.Core;
using FTOptix.NetLogic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Net.Http.Headers;
using System.Net;
using System.Threading.Tasks;
using System.Collections.Generic;
using FTOptix.System;
using FTOptix.EventLogger;
using FTOptix.Store;
using FTOptix.SQLiteStore;
using System.Net.Http.Json;
using Newtonsoft.Json;
#endregion

public class RuntimeNetLogic1 : BaseNetLogic
{
    private Device device;
    private string username;
    private string actualPassword;
    private string deviceIP;
    private string newPassword;

    public override void Start()
    {
        username = "admin";
        deviceIP = LogicObject.GetVariable("deviceIP").Value;
        newPassword = LogicObject.GetVariable("newPassword").Value;
        actualPassword = LogicObject.GetVariable("actualPassword").Value;
        device = new Device(deviceIP);
    }

    public override void Stop()
    {
        // Insert code to be executed when the user-defined logic is stopped
    }

    [ExportMethod]
    public void DeviceUpdatePassword()
    {
        LongRunningTask longRunningTask = new LongRunningTask(DeviceLoginAdUpdatePassword, LogicObject);
        longRunningTask.Start();
    }

    [ExportMethod]
    public void DeviceImportConfiguration()
    {

    }

    private async void DeviceLoginAdUpdatePassword()
    {
        var loginRes = await device.Login(username, actualPassword);
        if (loginRes.IsSuccessStatusCode)
        {
            var changePswRes = await device.ChangePassword(username, newPassword);
            if (changePswRes.IsSuccessStatusCode)
            {
                Log.Info("Password changed correctly");
            }
            else
            {
                Log.Error("Device change password error: " + changePswRes.StatusCode);
                return;
            }
        }
        else
        {
            Log.Error("Device login error: " + loginRes.StatusCode);
            return;
        }
    }
}

public class Device
{
    private readonly HttpClient _httpClient;
    private string _reachableIp;
    private string _password;
    private string _baseUrl;

    public Device(string reachableIp)
    {
        _baseUrl = $"https://{reachableIp}";

        // Configure HttpClient with a custom handler to ignore SSL warnings
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };
        _httpClient = new HttpClient(handler);

        // Set default headers
        _httpClient.DefaultRequestHeaders.Accept.Clear();
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public async Task<HttpResponseMessage> Login(string username, string password)
    {
        var payload = new { username, password };

        // Serialize the payload to JSON
        var jsonPayload = System.Text.Json.JsonSerializer.Serialize(payload);

        // Create the HTTP content
        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
        content.Headers.ContentType.CharSet = "";
        // content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        // Make the POST request
        var response = await _httpClient.PostAsync("https://192.168.0.10/login", content);

        foreach (var header in content.Headers)
        {
            Log.Info($"{header.Key}: {string.Join(", ", header.Value)}");
        }


        return response;
    }

    public async Task<HttpResponseMessage> RetrieveConfigurationsList()
    {
        var response = await _httpClient.GetAsync($"{_baseUrl}/api/deviceconfiguration/export");
        return response;
    }

    public async Task<HttpResponseMessage> ExportConfigurations(object jsonConfigurationsList)
    {
        var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/api/deviceconfiguration/export?location=web", jsonConfigurationsList);
        return response;
    }

    public async Task<HttpResponseMessage> ImportConfigurations(object jsonExportedConfigurations)
    {
        var response = await _httpClient.PatchAsync(
            $"{_baseUrl}/api/deviceconfiguration/import?location=web",
            JsonContent.Create(jsonExportedConfigurations)
        );
        return response;
    }

    public async Task<HttpResponseMessage> ChangePassword(string username, string newPassword)
    {
        var payload = new { username, password = newPassword };
        var response = await _httpClient.PatchAsync(
            $"{_baseUrl}/api/users/accounts",
            JsonContent.Create(payload)
        );

        return response;
    }
}

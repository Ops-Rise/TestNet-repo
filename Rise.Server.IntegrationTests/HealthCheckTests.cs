using System;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Rise.Server.IntegrationTests;

public class HealthCheckTests : IClassFixture<CustomWebApplicationFactory<Program>>{
    private readonly HttpClient _httpClient;
    public HealthCheckTests(CustomWebApplicationFactory<Program> factory){
        _httpClient = factory.CreateDefaultClient();
    }

    [Fact]  
    public async Task HealthCheck_ReturnsOk(){
        var response = await _httpClient.GetAsync("/healthcheck");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
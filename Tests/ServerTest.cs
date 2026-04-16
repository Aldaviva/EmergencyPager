using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Pager.Duty;
using System.Net;
using System.Net.Http.Headers;

namespace Tests;

/*
 * If you get either of the following errors:
 *   - System.ArgumentException : Argument --parentprocessid was not specified.
 *   - System.InvalidOperationException : Can't find 'C:\Users\Ben\Documents\Projects\FreshPager\Tests\bin\Debug\net8.0\testhost.deps.json'. This file is required for functional tests to run properly. There should be a copy of the file on your source project bin folder. If that is not the case, make sure that the property PreserveCompilationContext is set to true on your project file. E.g '<PreserveCompilationContext>true</PreserveCompilationContext>'. For functional tests to work they need to either run from the build output folder or the testhost.deps.json file from your application's output directory must be copied to the folder where the tests are running on. A common cause for this error is having shadow copying enabled when the tests run.
 *
 * then WebApplicationFactory is trying to use the wrong Program class, and you likely need to make Program visible to the Test project using ONE of the following techniques:
 *   - <InternalsVisibleTo Include="Tests" />
 *   - public partial class Program { }
 */
public class ServerTest: IDisposable {

    private static readonly MediaTypeHeaderValue JSON_TYPE = new("application/json");

    private readonly WebApplicationFactory<Program> webapp;
    private readonly IPagerDuty                     pagerDuty = A.Fake<IPagerDuty>();
    private readonly HttpClient                     testClient;

    public ServerTest() {
        if (typeof(Program).FullName == "Microsoft.VisualStudio.TestPlatform.TestHost.Program") {
            throw new InvalidOperationException(
                "Wrong Program class! Make sure Program is (partial) public, or internals are visible to test project. See https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests#basic-tests-with-the-default-webapplicationfactory");
        }

        webapp = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => {
                builder.UseTestServer();
                builder.UseEnvironment("Test");
                builder.ConfigureAppConfiguration(c => c.AddJsonFile("appsettings.Test.json", false, false));
            });

        testClient = webapp.CreateClient();
    }

    [Fact]
    public async Task methodNotAllowed() {
        using HttpResponseMessage response = await testClient.GetAsync("/pagerduty", TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed);
    }

    public void Dispose() {
        webapp.Dispose();
        testClient.Dispose();
        GC.SuppressFinalize(this);
    }

}
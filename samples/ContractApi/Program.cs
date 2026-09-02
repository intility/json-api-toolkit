using ContractApi;
using JsonApiToolkit.Extensions;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ContractDbContext>(o => o.UseInMemoryDatabase("contract-api"));
builder
    .Services.AddAuthentication("Test")
    .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, TestAuthHandler>(
        "Test",
        null
    );
builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddJsonApiToolkit(o =>
{
    // Contract tests run one default instance and one instance with every
    // opt-in behavior enabled.
    bool strict = builder.Configuration.GetValue<bool>("JSONAPI_STRICT");
    o.StrictPagination = strict;
    o.StrictQueryValidation = strict;
    o.PreserveQueryInPaginationLinks = strict;
});

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    Seed.Run(scope.ServiceProvider.GetRequiredService<ContractDbContext>());
}

app.Run();

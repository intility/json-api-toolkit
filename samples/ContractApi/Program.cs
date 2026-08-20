using ContractApi;
using JsonApiToolkit.Extensions;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ContractDbContext>(o => o.UseInMemoryDatabase("contract-api"));
builder.Services.AddControllers();
builder.Services.AddJsonApiToolkit(o =>
{
    // Contract tests run one fully strict instance and one default instance.
    bool strict = builder.Configuration.GetValue<bool>("JSONAPI_STRICT");
    o.StrictPagination = strict;
    o.StrictQueryValidation = strict;
});

var app = builder.Build();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    Seed.Run(scope.ServiceProvider.GetRequiredService<ContractDbContext>());
}

app.Run();

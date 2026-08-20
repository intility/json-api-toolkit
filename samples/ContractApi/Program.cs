using ContractApi;
using JsonApiToolkit.Extensions;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ContractDbContext>(o => o.UseInMemoryDatabase("contract-api"));
builder.Services.AddControllers();
builder.Services.AddJsonApiToolkit(o =>
{
    // Contract tests run one instance with strict pagination and one without.
    o.StrictPagination = builder.Configuration.GetValue<bool>("STRICT_PAGINATION");
});

var app = builder.Build();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    Seed.Run(scope.ServiceProvider.GetRequiredService<ContractDbContext>());
}

app.Run();

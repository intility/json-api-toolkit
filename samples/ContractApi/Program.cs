using ContractApi;
using JsonApiToolkit.Extensions;
using Microsoft.EntityFrameworkCore;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ContractDbContext>(o => o.UseInMemoryDatabase("contract-api"));
builder.Services.AddControllers();
builder.Services.AddJsonApiToolkit(o =>
{
    bool strict = builder.Configuration.GetValue<bool>("JSONAPI_STRICT");
    o.StrictPagination = strict;
    o.StrictQueryValidation = strict;
    o.PreserveQueryInPaginationLinks = strict;
    o.UseResourceAttributeTypeNames = strict;
});

WebApplication app = builder.Build();
app.MapControllers();

using (IServiceScope scope = app.Services.CreateScope())
{
    Seed.Run(scope.ServiceProvider.GetRequiredService<ContractDbContext>());
}

app.Run();

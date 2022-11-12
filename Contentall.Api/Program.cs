using Contentall.Api.Services.AccountServices.Authorization;
using Contentall.Api.Services.AccountServices;
using Contentall.Compilers.GraphQLCompiler;
using Contentall.Data.LiteDBProvider;
using Contentall.Data.Provider.Abstractions;
using Contentall.Security.Abstractions.Authorization;
using LiteDB;
using Contentall.Api.Services.EmailServices;
using Contentall.Security.Abstractions.Helpers;
using Contentall.Api.Extensions;

EntitiesContainer container;

void build()
{
    ModelCompiler compiler = new ModelCompiler(@"./models/Model.cs");
    var builder = WebApplication.CreateBuilder(args);
    builder.Services
        .AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies())
        .AddHttpContextAccessor()
        .Configure<AppSettings>(builder.Configuration.GetSection("AppSettings"))
        .AddLiteDBProvider(new ConnectionString() { Filename = "./data.dblite" })
        .AddScoped<IJwtUtils, JwtUtils>()
        .AddScoped<IAccountService, AccountService>()
        .AddScoped<IEmailService, EmailService>()
        .AddGraphQLServer()
        .AddQueryType<Query>()
        .AddMutationType<Mutation>()
        .AddTypeExtension<AccountServiceExtensions>()
        .AddFiltering();

    var app = builder.Build();
    app.MapGraphQL();
    container = app.Services.GetService<EntitiesContainer>();
    app.Run();
}

build();

//container.Insert(new Person() { id = Guid.NewGuid().ToString(), FirstName = "John", LastName = "Doe" });

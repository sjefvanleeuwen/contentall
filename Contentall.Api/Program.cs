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
using Blazor.AdminLte.UserApi.Helpers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

EntitiesContainer container;

void build()
{
    ModelCompiler compiler = new ModelCompiler(@"./models/Model.cs");
    var builder = WebApplication.CreateBuilder(args);

    var signingKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes("7G3A1pzULkCo0vsjI+vovw=="));
    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters =
                new TokenValidationParameters
                {
                    ValidIssuer = "https://auth.chillicream.com",
                    ValidAudience = "https://graphql.chillicream.com",
                    ValidateIssuerSigningKey = true,
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    IssuerSigningKey = signingKey
                };
        });
    builder.Services
        .AddAuthorization()
        .AddCors()
        .AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies())
        .AddHttpContextAccessor()
        .Configure<AppSettings>(builder.Configuration.GetSection("AppSettings"))
        .AddLiteDBProvider(new ConnectionString() { Filename = "./data.dblite" })

        .AddScoped<IJwtUtils, JwtUtils>()
        .AddScoped<IAccountService, AccountService>()
        .AddScoped<IEmailService, EmailService>()
        .AddGraphQLServer()
        .AddAuthorization()
        .AddQueryType<Query>()
        .AddMutationType<Mutation>()
        .AddTypeExtension<AccountServiceExtensions>()
        .AddFiltering();




    var app = builder.Build();
    app.MapGraphQL();
    //container = app.Services.GetService<EntitiesContainer>();
    app.UseCors(x => x
    .SetIsOriginAllowed(origin => true)
    .AllowAnyMethod()
    .AllowAnyHeader()
    .AllowCredentials());

    // global error handler
    app.UseMiddleware<ErrorHandlerMiddleware>();
    app.UseAuthorization();
    // custom jwt auth middleware
    app.UseMiddleware<JwtMiddleware>();
    app.Run();
}

build();

//container.Insert(new Person() { id = Guid.NewGuid().ToString(), FirstName = "John", LastName = "Doe" });

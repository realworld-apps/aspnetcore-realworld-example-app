using System;
using System.Collections.Generic;
using Conduit;
using Conduit.Infrastructure;
using Conduit.Infrastructure.Errors;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi;

// read database configuration (database provider + database connection) from environment variables
//Environment.GetEnvironmentVariable(DEFAULT_DATABASE_PROVIDER)
//Environment.GetEnvironmentVariable(DEFAULT_DATABASE_CONNECTION_STRING)
var defaultDatabaseConnectionString = "Filename=realworld.db";
var defaultDatabaseProvider = "sqlite";

var builder = WebApplication.CreateBuilder(args);

// take the connection string from the environment variable or use hard-coded database name
var connectionString = defaultDatabaseConnectionString;

// take the database provider from the environment variable or use hard-coded database provider
var databaseProvider = defaultDatabaseProvider;

builder.Services.AddDbContext<ConduitContext>(options =>
{
    if (databaseProvider.ToLowerInvariant().Trim().Equals("sqlite", StringComparison.Ordinal))
    {
        options.UseSqlite(connectionString);
    }
    else if (
        databaseProvider.ToLowerInvariant().Trim().Equals("sqlserver", StringComparison.Ordinal)
    )
    {
        // only works in windows container
        options.UseSqlServer(connectionString);
    }
    else
    {
        throw new InvalidOperationException(
            "Database provider unknown. Please check configuration"
        );
    }
});

builder.Services.AddLocalization(x => x.ResourcesPath = "Resources");

// Inject an implementation of ISwaggerProvider with defaulted settings applied
builder.Services.AddSwaggerGen(x =>
{
    x.AddSecurityDefinition(
        "Bearer",
        new OpenApiSecurityScheme
        {
            In = ParameterLocation.Header,
            Description = "Please insert JWT with Bearer into field",
            Name = "Authorization",
            Type = SecuritySchemeType.ApiKey,
            BearerFormat = "JWT",
        }
    );

    x.SupportNonNullableReferenceTypes();

    x.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = new List<string>(),
    });
    x.SwaggerDoc("v1", new OpenApiInfo { Title = "RealWorld API", Version = "v1" });
    x.CustomSchemaIds(y => y.FullName);
    x.DocInclusionPredicate((_, _) => true);
    x.TagActionsBy(y => new List<string> { y.GroupName ?? throw new InvalidOperationException() });
    x.CustomSchemaIds(s => s.FullName?.Replace("+", "."));
});

builder.Services.AddCors();
builder
    .Services.AddMvc(opt =>
    {
        opt.Conventions.Add(new GroupByApiRootConvention());
        // the RealWorld API spec mounts all endpoints under /api
        opt.Conventions.Add(
            new ApiRoutePrefixConvention(builder.Configuration["ApiPrefix"] ?? "api")
        );
        opt.Filters.Add<ValidatorActionFilter>();
        opt.EnableEndpointRouting = false;
    })
    // the RealWorld spec expects nullable fields (bio, image, ...) to be serialized as explicit nulls
    .AddJsonOptions(opt =>
        opt.JsonSerializerOptions.DefaultIgnoreCondition = System
            .Text
            .Json
            .Serialization
            .JsonIgnoreCondition
            .Never
    );

builder.Services.AddConduit();

builder.Services.AddJwt();

var app = builder.Build();

app.Services.GetRequiredService<ILoggerFactory>().AddSerilogLogging();

app.UseMiddleware<ErrorHandlingMiddleware>();

app.UseCors(x => x.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());

app.UseAuthentication();
app.UseMvc();

// Enable middleware to serve generated Swagger as a JSON endpoint
app.UseSwagger(c => c.RouteTemplate = "swagger/{documentName}/swagger.json");

// Enable middleware to serve swagger-ui assets(HTML, JS, CSS etc.)
app.UseSwaggerUI(x => x.SwaggerEndpoint("/swagger/v1/swagger.json", "RealWorld API V1"));

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope
        .ServiceProvider.GetRequiredService<ConduitContext>()
        .Database.EnsureCreated();
    // use context
}
app.Run();

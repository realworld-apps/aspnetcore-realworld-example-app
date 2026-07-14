using System;
using System.Collections.Generic;
using System.Diagnostics;
using Conduit;
using Conduit.Infrastructure;
using Conduit.Infrastructure.Errors;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using OpenTelemetry.Exporter;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

// read database configuration (database provider + database connection) from environment variables
//Environment.GetEnvironmentVariable(DEFAULT_DATABASE_PROVIDER)
//Environment.GetEnvironmentVariable(DEFAULT_DATABASE_CONNECTION_STRING)
var defaultDatabaseConnectionString = Environment.GetEnvironmentVariable("DEFAULT_DATABASE_CONNECTION_STRING");
var defaultDatabaseProvider = "postgres";
//var defaultDatabaseConnectionString = "Filename=realworld.db";
//var defaultDatabaseProvider = "sqlite";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
var configuration = builder.Configuration;
// take the connection string from the environment variable or use hard-coded database name
var connectionString = defaultDatabaseConnectionString;

// take the database provider from the environment variable or use hard-coded database provider
var databaseProvider = defaultDatabaseProvider;
builder.Services.AddDbContext<ConduitContext>(options =>
{
    if (databaseProvider.Equals("postgres", StringComparison.OrdinalIgnoreCase) || databaseProvider.Equals("postgresql", StringComparison.OrdinalIgnoreCase))
    {
        options.UseNpgsql(connectionString);
    }else
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

//This is only code which trace the application excuetion.... 
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService("ConduitAPI", serviceVersion: "1.0.0"))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation(options =>
        {
            options.EnrichWithHttpRequest = (activity, request) =>
            {
                activity.SetTag("aws.ec2.instance_id", configuration["AWS:Instance:Id"]);
                activity.SetTag("aws.ec2.instance_type", configuration["AWS:Instance:Type"]);
                activity.SetTag("aws.ec2.license_model", configuration["AWS:Instance:LicenseModel"]);
                activity.SetTag("aws.ec2.operating_system", configuration["AWS:Instance:OperatingSystem"]);
                activity.SetTag("aws.ec2.tenancy", configuration["AWS:Instance:Tenancy"]);
                activity.SetTag("aws.ec2.deployment.option", configuration["AWS:Instance:DeploymentOption"]);

                // RDS Metadata
                activity.SetTag("aws.rds.engine", configuration["AWS:RDS:Engine"]);
                activity.SetTag("aws.rds.engine_version", configuration["AWS:RDS:EngineVersion"]);
                activity.SetTag("aws.rds.instance.class", configuration["AWS:RDS:InstanceClass"]);
                activity.SetTag("aws.rds.instance.id", configuration["AWS:RDS:InstanceId"]);
                activity.SetTag("aws.rds.license.model", configuration["AWS:RDS:LicenseModel"]);
                activity.SetTag("aws.rds.storage.type", configuration["AWS:RDS:StorageType"]);

                // Region and Cost
                activity.SetTag("aws.region", configuration["AWS:Region"]);
                activity.SetTag("cost.estimate_usd", configuration["Cost:EstimateUSD"]);
            };
        })
        .AddOtlpExporter(options =>
        {
            options.Endpoint = new Uri("https://otel.beakpointinsights.com/api/traces");
            options.Headers = $"x-bkpt-key={Environment.GetEnvironmentVariable("BREAKPOINT_API_KEY")}";
            options.Protocol = OtlpExportProtocol.HttpProtobuf;
        })
        .AddConsoleExporter());





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

    x.AddSecurityRequirement(
        new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer",
                    },
                },
                Array.Empty<string>()
            },
        }
    );
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
        opt.Filters.Add<ValidatorActionFilter>();
        opt.EnableEndpointRouting = false;
    })
    .AddJsonOptions(opt =>
        opt.JsonSerializerOptions.DefaultIgnoreCondition = System
            .Text
            .Json
            .Serialization
            .JsonIgnoreCondition
            .WhenWritingNull
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

var logger = app.Services.GetRequiredService<ILogger<Program>>();

app.Run();

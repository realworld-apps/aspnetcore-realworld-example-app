using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Amazon;
using Amazon.EC2;
using Amazon.EC2.Model;
using Amazon.Util;
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

var otlpEndpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT") ?? "http://localhost:4317";
var deploymentEnvironment = Environment.GetEnvironmentVariable("DEPLOYMENT_ENVIRONMENT") ?? "development";

var ec2Attributes = GetEc2Attributes();

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService("realworld-demo", serviceVersion: "1.0.0")
        .AddAttributes(new Dictionary<string, object>
        {
            ["deployment.environment"] = deploymentEnvironment
        })
        .AddAttributes(ec2Attributes))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation(options =>
        {
            options.EnrichWithHttpRequest = (activity, _) =>
            {
                foreach (var attr in ec2Attributes)
                {
                    activity.SetTag(attr.Key, attr.Value);
                }
            };
        })
        .AddHttpClientInstrumentation(options =>
        {
            options.EnrichWithHttpRequestMessage = (activity, _) =>
            {
                foreach (var attr in ec2Attributes)
                {
                    activity.SetTag(attr.Key, attr.Value);
                }
            };
        })
        .AddSource("Microsoft.EntityFrameworkCore")
        .AddOtlpExporter(options =>
        {
            options.Endpoint = new Uri(otlpEndpoint);
        }));





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

static Dictionary<string, object> GetEc2Attributes()
{
    try
    {
        var instanceId = EC2InstanceMetadata.InstanceId;
        if (string.IsNullOrEmpty(instanceId))
        {
            return new Dictionary<string, object>();
        }

        var regionName = EC2InstanceMetadata.AvailabilityZone[..^1];
        var region = RegionEndpoint.GetBySystemName(regionName);
        var client = new AmazonEC2Client(region);

        var response = client.DescribeInstancesAsync(
            new DescribeInstancesRequest { InstanceIds = [instanceId] }).GetAwaiter().GetResult();

        var instance = response.Reservations.First().Instances.First();

        var attributes = new Dictionary<string, object>
        {
            ["cloud.platform"] = "aws_ec2",
            ["host.id"] = instance.InstanceId,
            ["host.type"] = instance.InstanceType.Value,
            ["cloud.region"] = regionName,
            ["os.type"] = instance.PlatformDetails.Contains("Windows", StringComparison.OrdinalIgnoreCase)
                ? "windows" : "linux",
            ["aws.ec2.license_model"] = instance.Licenses is null || instance.Licenses.Count == 0
                ? "No License required" : "Bring your own license",
            ["aws.ec2.tenancy"] = instance.Placement.Tenancy.Value
        };

        if (instance.PlatformDetails != "Linux/UNIX" && instance.PlatformDetails != "Windows")
        {
            attributes["aws.ec2.platform_details"] = instance.PlatformDetails;
        }

        if (instance.InstanceLifecycle is not null)
        {
            attributes["aws.ec2.instance_lifecycle"] = instance.InstanceLifecycle.Value;
        }

        if (instance.CapacityReservationId is not null)
        {
            attributes["aws.ec2.capacity_reservation_id"] = instance.CapacityReservationId;
        }

        return attributes;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Warning: Could not fetch EC2 metadata: {ex.Message}");
        return new Dictionary<string, object>();
    }
}

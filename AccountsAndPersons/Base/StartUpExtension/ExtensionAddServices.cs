using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;

namespace AccountsAndPersons
{
    public static class ExtensionAddServices
    {

        //public static void AddRedisDependencyInjection(this IServiceCollection services, IConfiguration Configuration)
        //{
        //    //redis 
        //    var configurationOptions = new ConfigurationOptions();
        //    configurationOptions.EndPoints.Add(Configuration["Redis:Host"], Convert.ToInt32(Configuration["Redis:Port"]));
        //    int.TryParse(Configuration["Redis:DefaultDatabase"], out int defaultDatabase);
        //    configurationOptions.DefaultDatabase = defaultDatabase;
        //    services.AddStackExchangeRedisCache(options =>
        //    {
        //        options.ConfigurationOptions = configurationOptions;
        //        options.InstanceName = Configuration["Redis:InstanceName"];
        //    });
        //}


        public static void AddServicesDependencyInjection(this IServiceCollection services)
        {
            services.AddSingleton<DapperDbContext>();
            services.AddScoped<AccountRepository>();
            services.AddScoped<PersonRepository>();

            services.AddScoped<UnitOfWork>();
        }

        public static void AddCustomizeSwagger(this IServiceCollection services)
        {
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Protein Api Management", Version = "v1.0" });
                c.OperationFilter<ExtensionSwaggerFileOperationFilter>();

                var securityScheme = new OpenApiSecurityScheme
                {
                    Name = "Protein Management for IT Company",
                    Description = "Enter JWT Bearer token **_only_**",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    Reference = new OpenApiReference
                    {
                        Id = JwtBearerDefaults.AuthenticationScheme,
                        Type = ReferenceType.SecurityScheme
                    }
                };
                c.AddSecurityDefinition(securityScheme.Reference.Id, securityScheme);
                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {securityScheme, new string[] { }}
                });
            });
        }

    }
}

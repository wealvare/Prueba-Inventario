using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Inventario.Api.Correlacion;
using Inventario.Api.Documentacion;
using Inventario.Api.Errores;
using Inventario.Infraestructura.Configuracion;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

namespace Inventario.Api;

public static class RegistroApi
{
    public const string PoliticaInicioSesion = "InicioSesion";
    public const string PoliticaCors = "Frontend";

    private static readonly TimeSpan ToleranciaReloj = TimeSpan.FromSeconds(30);

    public static IServiceCollection AgregarApi(this IServiceCollection servicios, IConfiguration configuracion)
    {
        AgregarControladores(servicios);
        AgregarAutenticacion(servicios);
        AgregarLimitePeticiones(servicios, configuracion);
        AgregarCors(servicios, configuracion);
        AgregarDocumentacion(servicios);
        servicios.AddResponseCompression(opciones =>
            opciones.MimeTypes = [.. ResponseCompressionDefaults.MimeTypes, Problemas.TipoContenido]);

        return servicios;
    }

    private static void AgregarControladores(IServiceCollection servicios)
    {
        servicios.AddControllers(opciones =>
        {
            opciones.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
            TraducirMensajes(opciones.ModelBindingMessageProvider);
        }).AddJsonOptions(opciones => opciones.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
        servicios.Configure<ApiBehaviorOptions>(opciones =>
        {
            opciones.SuppressMapClientErrors = true;
            opciones.InvalidModelStateResponseFactory = RespuestaModeloInvalido.Crear;
        });
    }

    private static void TraducirMensajes(DefaultModelBindingMessageProvider mensajes)
    {
        mensajes.SetAttemptedValueIsInvalidAccessor((valor, campo) => $"El valor '{valor}' no es válido para {campo}.");
        mensajes.SetNonPropertyAttemptedValueIsInvalidAccessor(valor => $"El valor '{valor}' no es válido.");
        mensajes.SetUnknownValueIsInvalidAccessor(campo => $"El valor indicado no es válido para {campo}.");
        mensajes.SetNonPropertyUnknownValueIsInvalidAccessor(() => "El valor indicado no es válido.");
        mensajes.SetValueIsInvalidAccessor(valor => $"El valor '{valor}' no es válido.");
        mensajes.SetValueMustBeANumberAccessor(campo => $"El campo {campo} debe ser un número.");
        mensajes.SetNonPropertyValueMustBeANumberAccessor(() => "El valor debe ser un número.");
        mensajes.SetValueMustNotBeNullAccessor(_ => "El valor es obligatorio.");
        mensajes.SetMissingBindRequiredValueAccessor(campo => $"El campo {campo} es obligatorio.");
        mensajes.SetMissingKeyOrValueAccessor(() => "El valor es obligatorio.");
        mensajes.SetMissingRequestBodyRequiredValueAccessor(() => "El cuerpo de la solicitud es obligatorio.");
    }

    private static void AgregarAutenticacion(IServiceCollection servicios)
    {
        servicios.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        servicios.AddAuthorization();
        servicios.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<OpcionesJwt>((opciones, jwt) =>
            {
                opciones.MapInboundClaims = false;
                opciones.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Emisor,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audiencia,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Clave)),
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                    ValidateLifetime = true,
                    RequireExpirationTime = true,
                    ClockSkew = ToleranciaReloj,
                    NameClaimType = "unique_name",
                    RoleClaimType = "role"
                };
            });
    }

    private static void AgregarLimitePeticiones(IServiceCollection servicios, IConfiguration configuracion)
    {
        var peticionesPorMinuto = configuracion.GetValue("LimiteInicioSesion:PeticionesPorMinuto", 5);

        servicios.AddRateLimiter(opciones =>
        {
            opciones.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            opciones.OnRejected = (rechazo, _) =>
            {
                if (rechazo.Lease.TryGetMetadata(MetadataName.RetryAfter, out var espera))
                    rechazo.HttpContext.Response.Headers.RetryAfter = ((int)Math.Ceiling(espera.TotalSeconds)).ToString();

                return ValueTask.CompletedTask;
            };
            opciones.AddPolicy(PoliticaInicioSesion, contexto => RateLimitPartition.GetFixedWindowLimiter(
                contexto.Connection.RemoteIpAddress?.ToString() ?? string.Empty,
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = peticionesPorMinuto,
                    Window = TimeSpan.FromMinutes(1)
                }));
        });
    }

    private static void AgregarCors(IServiceCollection servicios, IConfiguration configuracion)
    {
        var origenes = configuracion.GetSection("Cors:OrigenesPermitidos").Get<string[]>() ?? [];

        servicios.AddCors(opciones => opciones.AddPolicy(PoliticaCors, politica => politica
            .WithOrigins(origenes)
            .AllowAnyHeader()
            .WithMethods(HttpMethods.Get, HttpMethods.Post, HttpMethods.Put, HttpMethods.Delete)
            .WithExposedHeaders("Location", "Retry-After", MiddlewareCorrelacion.Encabezado)));
    }

    private static void AgregarDocumentacion(IServiceCollection servicios)
    {
        servicios.AddSwaggerGen(opciones =>
        {
            opciones.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Inventario de productos",
                Version = "v1",
                Description = "API para administrar productos y sus lotes."
            });
            opciones.AddSecurityDefinition(FiltroSeguridad.Esquema, new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "Pega el token obtenido al iniciar sesión."
            });
            opciones.EnableAnnotations();
            opciones.SchemaFilter<FiltroEjemplos>();
            opciones.OperationFilter<FiltroParametros>();
            opciones.OperationFilter<FiltroSeguridad>();
        });
    }
}

using Microsoft.Extensions.DependencyInjection;

EwfOps.RunApplication( new GlobalInitializer(), dependencyInjectionServicesRegistrationMethod: services => { services.AddControllers(); } );
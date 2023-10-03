namespace GatewayService.StartUp;

public static class ConfigurationInitializer
{
    /// <summary>
    /// Extension class that adds the ConfigureAppSettings to the IHostBuilder interface. It is used to abstract the 
    /// config logic out of the program.cs class and make it reusable. We can now call the "ConfigureAppSettings()" on 
    /// the IHostBuilder instance in the program.cs class. 
    /// </summary>


    // bool that we use to configure whether or not the file has to exists. False means it has to exist, or the 
    // application with throw an error. 
    private static readonly bool enableSettings = false;

    // extension to IHostBuilder
    public static IHostBuilder ConfigureAppSettings(this IHostBuilder host)
    {
        // get the environment value, to be used to specify what environment we are currently in. (prod, dev, test)
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        var machineName = Environment.MachineName;


        // method to configure the settings of the app. It takes an Action as parameter (the host context "ctx") and the 
        // configbuilder "builder" as parameters. 
        host.ConfigureAppConfiguration((ctx, builder) =>
        {
            // add the configuration source to the builder. In this case "appsettings.json". 
            // "enablseSettings" is passed here to tell wether or not the file has to exists.
            // reloadOnChange = true means that the file must be reread if changed.
            builder.AddJsonFile("appsettings.json", enableSettings, true);

            // same as above but taking into account the environment we are in. optional = true because we only want
            // to read in the settings from the appsettings.environment if the file exists. 
            builder.AddJsonFile($"appsettings.{environment}.json", true, true);

            // same as above but for any specific machine.
            builder.AddJsonFile($"appsettings.{machineName}.json", true, true);

            // this line is to be commented out if the local server is not running. 
            //builder.AddJsonFile($"appsettings.ubuntu-local-server.json", true, true);


            // adding the envinronmental variables as a config source.  
            builder.AddEnvironmentVariables();
        });

        // returning the host.
        return host;
    }
}
using MongoDB.Driver;
using System.Security.Authentication;
using Microsoft.Extensions.Configuration;

namespace XchangeCrypt.Backend.TradingBackend.Repositories
{
    public class DataAccess
    {
        public IMongoClient MongoClient { get; }
        public IMongoDatabase Database { get; }

        public DataAccess(IConfiguration configuration)
        {
            var settings = MongoClientSettings.FromUrl(
                new MongoUrl(configuration["Database:ConnectionString"])
            );
            settings.SslSettings = new SslSettings {EnabledSslProtocols = SslProtocols.Tls12};
            MongoClient = new MongoClient(settings);
            Database = MongoClient.GetDatabase("XchangeCrypt");
        }
    }
}

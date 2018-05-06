using MongoDB.Driver;
using System.Security.Authentication;

namespace XchangeCrypt.Backend.TradingBackend.Repositories
{
    public class DataAccess
    {
        public IMongoClient MongoClient { get; }
        public IMongoDatabase Database { get; }

        public DataAccess()
        {
            string connectionString = @"mongodb://xchangecrypt-test:nPtzeE05tNvlf0xNisV9erizFn8EtiAT3WknfGNxlyduMLZAzObdcyppah0Y23MIcRT83euXhcv36dW66ltNdA==@xchangecrypt-test.documents.azure.com:10255/?ssl=true&replicaSet=globaldb";
            MongoClientSettings settings = MongoClientSettings.FromUrl(
                new MongoUrl(connectionString)
            );
            settings.SslSettings = new SslSettings() { EnabledSslProtocols = SslProtocols.Tls12 };
            MongoClient = new MongoClient(settings);
            Database = MongoClient.GetDatabase("XchangeCrypt");
        }
    }
}

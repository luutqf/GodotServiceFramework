// namespace GodotServiceFramework.Http.Client;
//
// public class SimpleHttpClient
// {
//     public static HttpClient CreateHttpClient(bool ignoreSslErrors = false, int timeoutSeconds = 5)
//     {
//         var handler = new HttpClientHandler();
//
//         if (ignoreSslErrors)
//         {
//             handler.ServerCertificateCustomValidationCallback =
//                 (message, cert, chain, errors) => true;
//         }
//
//         var client = new HttpClient(handler);
//         client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
//         return client;
//     }
// }
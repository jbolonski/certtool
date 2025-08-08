using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;

namespace Server.Services;

public class CertificateFetcher
{
    public async Task<(string serial, DateTime notAfterUtc)?> FetchAsync(string host, int port = 443, CancellationToken ct = default)
    {
        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync(host, port, ct);
            using var ssl = new SslStream(client.GetStream(), false, (sender, cert, chain, errors) => true);
            await ssl.AuthenticateAsClientAsync(host);
            var cert2 = new X509Certificate2(ssl.RemoteCertificate!);
            return (cert2.SerialNumber, cert2.NotAfter.ToUniversalTime());
        }
        catch
        {
            return null;
        }
    }
}

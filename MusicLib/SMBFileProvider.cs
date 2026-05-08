
using SharpCifs.Smb;

namespace MusicLib
{
    public class FileProvider
    {
        public virtual Stream GetFileStream(string filePath)
        {
            throw new NotImplementedException();
        }
    }

    public class SMBFileProvider : FileProvider
    {
        NtlmPasswordAuthentication auth;
        string serverPath;

        public SMBFileProvider(string serverPath, string domain, string user, string password)
        {
            this.serverPath = serverPath;

            auth = new NtlmPasswordAuthentication(domain, user, password);
        }

        public override Stream GetFileStream(string filePath)
        {
            var smbFile = new SmbFile(Path.Combine(serverPath, filePath), auth);

            return smbFile.GetInputStream();
        }
    }
}
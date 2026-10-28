
using SharpCifs.Smb;

namespace MusicLib
{
    public class FileProvider
    {
        public virtual Stream GetInputStream(string filePath)
        {
            throw new NotImplementedException();
        }

        public virtual void WriteTextToFile(string text, string filePath)
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

        public override Stream GetInputStream(string filePath)
        {
            var smbFile = new SmbFile(Path.Combine(serverPath, filePath), auth);

            return smbFile.GetInputStream();
        }

        public override void WriteTextToFile(string text, string filePath)
        {
            var smbFile = new SmbFile(Path.Combine(serverPath, filePath), auth);

            if (smbFile.Exists())
            {
                smbFile.Delete();
            }

            smbFile.CreateNewFile();

            using (StreamWriter writer = new StreamWriter(smbFile.GetOutputStream()))
            {
                writer.Write(text);
            }
        }
    }
}
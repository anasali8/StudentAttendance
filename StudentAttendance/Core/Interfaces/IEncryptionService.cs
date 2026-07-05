namespace StudentAttendance.Core.Interfaces;

public interface IEncryptionService
{
    byte[] Encrypt(byte[] plainData);
    byte[] Decrypt(byte[] encryptedData);
    
    string EncryptString(string plainText);
    string DecryptString(string encryptedText);
}

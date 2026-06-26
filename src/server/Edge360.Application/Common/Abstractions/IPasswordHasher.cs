namespace Edge360.Application.Common.Abstractions;

/// <summary>Hashes and verifies user passwords using a salted, iterated KDF.</summary>
public interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(string hash, string password);
}

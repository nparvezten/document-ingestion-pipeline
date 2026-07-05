using System;
using DocumentIngestion.Api.Services;
using Xunit;

namespace DocumentIngestion.Tests;

public class SecurityTests
{
    private readonly SecurityHelper _securityHelper;

    public SecurityTests()
    {
        // Setup SecurityHelper using fallback hex keys (32 bytes)
        string testAesKey = "11223344556677889900aabbccddeeff11223344556677889900aabbccddeeff";
        string testHmacKey = "ffeeeeddccbbaa998877665544332211ffeeeeddccbbaa998877665544332211";
        _securityHelper = new SecurityHelper(testAesKey, testHmacKey);
    }

    [Fact]
    public void Encrypt_And_Decrypt_Succeeds()
    {
        string originalText = "Aadhaar-1234-5678-9012";
        string cipherText = _securityHelper.Encrypt(originalText);

        Assert.NotEmpty(cipherText);
        Assert.NotEqual(originalText, cipherText);

        string decryptedText = _securityHelper.Decrypt(cipherText);
        Assert.Equal(originalText, decryptedText);
    }

    [Fact]
    public void Encryption_Is_Semantic_Using_Random_IV()
    {
        string originalText = "INV-987654";
        
        string cipher1 = _securityHelper.Encrypt(originalText);
        string cipher2 = _securityHelper.Encrypt(originalText);

        // Encrypting the same text twice MUST produce different ciphertext values due to random IVs.
        Assert.NotEqual(cipher1, cipher2);

        // Both must decrypt back to the identical plaintext
        Assert.Equal(originalText, _securityHelper.Decrypt(cipher1));
        Assert.Equal(originalText, _securityHelper.Decrypt(cipher2));
    }

    [Fact]
    public void BlindIndex_Is_Deterministic()
    {
        string input1 = "INV-987654";
        string input2 = "inv-987654 "; // slight casing & spacing variation

        string index1 = _securityHelper.ComputeBlindIndex(input1);
        string index2 = _securityHelper.ComputeBlindIndex(input2);

        Assert.NotEmpty(index1);
        // The blind index must be deterministic and match regardless of casing/spacing normalization.
        Assert.Equal(index1, index2);

        string diffInput = "INV-987655";
        string diffIndex = _securityHelper.ComputeBlindIndex(diffInput);
        
        Assert.NotEqual(index1, diffIndex);
    }
}

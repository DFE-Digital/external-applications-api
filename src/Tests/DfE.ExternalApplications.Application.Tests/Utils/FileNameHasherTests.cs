using DfE.ExternalApplications.Utils.File;

namespace DfE.ExternalApplications.Application.Tests.Utils;

public class FileNameHasherTests
{
    [Fact]
    public void HashFileName_ShouldReturnHashedName_WithOriginalExtension()
    {
        // Act
        var result = FileNameHasher.HashFileName("document.pdf");

        // Assert
        Assert.EndsWith(".pdf", result);
        Assert.NotEqual("document.pdf", result);
    }

    [Fact]
    public void HashFileName_ShouldReturnConsistentHash_ForSameName()
    {
        // Act
        var result1 = FileNameHasher.HashFileName("test.txt");
        var result2 = FileNameHasher.HashFileName("test.txt");

        // Assert
        Assert.Equal(result1, result2);
    }

    [Fact]
    public void HashFileName_ShouldReturnDifferentHash_ForDifferentNames()
    {
        // Act
        var result1 = FileNameHasher.HashFileName("file1.txt");
        var result2 = FileNameHasher.HashFileName("file2.txt");

        // Assert
        Assert.NotEqual(result1, result2);
    }

    [Fact]
    public void HashFileName_ShouldPreserveExtension()
    {
        // Act
        var resultPdf = FileNameHasher.HashFileName("doc.pdf");
        var resultDocx = FileNameHasher.HashFileName("doc.docx");
        var resultPng = FileNameHasher.HashFileName("image.png");

        // Assert
        Assert.EndsWith(".pdf", resultPdf);
        Assert.EndsWith(".docx", resultDocx);
        Assert.EndsWith(".png", resultPng);
    }

    [Fact]
    public void HashFileName_ShouldReturnLowercaseHex_WithExtension()
    {
        // Act
        var result = FileNameHasher.HashFileName("SomeFile.txt");

        // Assert
        var hashPart = result[..^4]; // Remove .txt
        Assert.Matches("^[0-9a-f]+$", hashPart);
    }

    [Fact]
    public void HashFileName_ShouldHandleFileWithNoExtension()
    {
        // Act
        var result = FileNameHasher.HashFileName("noextension");

        // Assert
        Assert.NotEmpty(result);
        Assert.DoesNotContain(".", result);
    }

    [Fact]
    public void HashFileName_ShouldReturnSameHash_ForSameNameWithDifferentExtensions()
    {
        // The hash is based only on the name part (without extension)
        var result1 = FileNameHasher.HashFileName("document.pdf");
        var result2 = FileNameHasher.HashFileName("document.txt");

        // The hash parts should be the same (only extension differs)
        var hash1 = System.IO.Path.GetFileNameWithoutExtension(result1);
        var hash2 = System.IO.Path.GetFileNameWithoutExtension(result2);

        Assert.Equal(hash1, hash2);
    }
}

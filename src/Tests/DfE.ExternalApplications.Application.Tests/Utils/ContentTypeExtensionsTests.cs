using DfE.ExternalApplications.Utils.File;
using Microsoft.AspNetCore.Http;
using NSubstitute;

namespace DfE.ExternalApplications.Application.Tests.Utils;

public class ContentTypeExtensionsTests
{
    [Theory]
    [InlineData("file.pdf", "application/pdf")]
    [InlineData("file.jpg", "image/jpeg")]
    [InlineData("file.png", "image/png")]
    [InlineData("file.txt", "text/plain")]
    [InlineData("file.html", "text/html")]
    [InlineData("file.json", "application/json")]
    [InlineData("file.docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document")]
    public void GetContentType_ShouldReturnCorrectMimeType_ForKnownExtensions(string fileName, string expectedContentType)
    {
        // Act
        var result = fileName.GetContentType();

        // Assert
        Assert.Equal(expectedContentType, result);
    }

    [Fact]
    public void GetContentType_ShouldReturnOctetStream_ForUnknownExtension()
    {
        // Act
        var result = "file.xyz123".GetContentType();

        // Assert
        Assert.Equal("application/octet-stream", result);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    public void GetContentType_ShouldThrowArgumentException_WhenFileNameIsNullOrWhitespace(string? fileName)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => fileName!.GetContentType());
    }

    [Fact]
    public void GetContentType_ForFormFile_ShouldReturnCorrectMimeType()
    {
        // Arrange
        var formFile = Substitute.For<IFormFile>();
        formFile.FileName.Returns("document.pdf");

        // Act
        var result = formFile.GetContentType();

        // Assert
        Assert.Equal("application/pdf", result);
    }

    [Fact]
    public void GetContentType_ForFormFile_ShouldThrowArgumentNullException_WhenFileIsNull()
    {
        // Arrange
        IFormFile? file = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => file!.GetContentType());
    }
}

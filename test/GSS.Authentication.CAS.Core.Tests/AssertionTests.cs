using GSS.Authentication.CAS.Security;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace GSS.Authentication.CAS.Core.Tests;

public class AssertionTests
{
    #region Constructor Validation Tests

    [Fact]
    public void Constructor_WithValidPrincipalName_ShouldCreateAssertion()
    {
        // Arrange
        var principalName = "testuser";

        // Act
        var assertion = new Assertion(principalName);

        // Assert
        Assert.Equal(principalName, assertion.PrincipalName);
        Assert.NotNull(assertion.Attributes);
        Assert.Empty(assertion.Attributes);
    }

    [Fact]
    public void Constructor_WithNullPrincipalName_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => new Assertion(null!));
    }

    [Fact]
    public void Constructor_WithEmptyPrincipalName_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => new Assertion(string.Empty));
    }

    [Fact]
    public void Constructor_WithWhitespacePrincipalName_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => new Assertion("   "));
    }

    #endregion

    #region Attributes Tests

    [Fact]
    public void Constructor_WithAttributes_ShouldStoreAttributes()
    {
        // Arrange
        var principalName = "testuser";
        var attributes = new Dictionary<string, StringValues>
        {
            ["email"] = new StringValues("test@example.com"),
            ["roles"] = new StringValues(["admin", "user"])
        };

        // Act
        var assertion = new Assertion(principalName, attributes);

        // Assert
        Assert.Equal(principalName, assertion.PrincipalName);
        Assert.Equal(2, assertion.Attributes.Count);
        Assert.Equal("test@example.com", assertion.Attributes["email"].ToString());
        Assert.Equal(2, assertion.Attributes["roles"].Count);
    }

    [Fact]
    public void Constructor_WithNullAttributes_ShouldCreateEmptyDictionary()
    {
        // Arrange
        var principalName = "testuser";

        // Act
        var assertion = new Assertion(principalName, null);

        // Assert
        Assert.NotNull(assertion.Attributes);
        Assert.Empty(assertion.Attributes);
    }

    [Fact]
    public void Constructor_WithEmptyAttributes_ShouldStoreEmptyDictionary()
    {
        // Arrange
        var principalName = "testuser";
        var attributes = new Dictionary<string, StringValues>();

        // Act
        var assertion = new Assertion(principalName, attributes);

        // Assert
        Assert.NotNull(assertion.Attributes);
        Assert.Empty(assertion.Attributes);
    }

    [Fact]
    public void Attributes_ShouldBeMutable()
    {
        // Arrange
        var assertion = new Assertion("testuser");

        // Act
        assertion.Attributes["newKey"] = new StringValues("newValue");

        // Assert
        Assert.Single(assertion.Attributes);
        Assert.Equal("newValue", assertion.Attributes["newKey"].ToString());
    }

    #endregion

    #region Property Immutability Tests

    [Fact]
    public void PrincipalName_ShouldBeReadOnly()
    {
        // Arrange
        var assertion = new Assertion("testuser");

        // Assert - PrincipalName has no setter, this is a compile-time check
        // The property should only have a get accessor
        Assert.Equal("testuser", assertion.PrincipalName);
    }

    #endregion
}

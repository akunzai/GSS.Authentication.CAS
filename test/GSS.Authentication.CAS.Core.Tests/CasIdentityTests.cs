using GSS.Authentication.CAS.Security;
using Microsoft.Extensions.Primitives;
using System.Security.Claims;
using Xunit;

namespace GSS.Authentication.CAS.Core.Tests;

public class CasIdentityTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidAssertion_ShouldCreateIdentity()
    {
        // Arrange
        var assertion = new Assertion("testuser");
        var authenticationType = "CAS";

        // Act
        var identity = new CasIdentity(assertion, authenticationType);

        // Assert
        Assert.NotNull(identity);
        Assert.Equal(assertion, identity.Assertion);
        Assert.Equal(authenticationType, identity.AuthenticationType);
        Assert.True(identity.IsAuthenticated);
    }

    [Fact]
    public void Constructor_WithNullAssertion_ShouldCreateIdentityWithNullAssertion()
    {
        // Arrange & Act
        // Note: CasIdentity does not validate null assertion in constructor
        var identity = new CasIdentity(null!, "CAS");

        // Assert
        Assert.NotNull(identity);
        Assert.Null(identity.Assertion);
    }

    [Fact]
    public void Constructor_WithEmptyAuthenticationType_ShouldCreateUnauthenticatedIdentity()
    {
        // Arrange
        var assertion = new Assertion("testuser");

        // Act
        var identity = new CasIdentity(assertion, string.Empty);

        // Assert
        Assert.NotNull(identity);
        Assert.Equal(assertion, identity.Assertion);
        Assert.Equal(string.Empty, identity.AuthenticationType);
        Assert.False(identity.IsAuthenticated);
    }

    [Fact]
    public void Constructor_WithNullAuthenticationType_ShouldCreateUnauthenticatedIdentity()
    {
        // Arrange
        var assertion = new Assertion("testuser");

        // Act
        var identity = new CasIdentity(assertion, null!);

        // Assert
        Assert.NotNull(identity);
        Assert.Equal(assertion, identity.Assertion);
        Assert.Null(identity.AuthenticationType);
        Assert.False(identity.IsAuthenticated);
    }

    #endregion

    #region Assertion Tests

    [Fact]
    public void Assertion_WithPrincipalNameOnly_ShouldBeAccessible()
    {
        // Arrange
        var principalName = "testuser";
        var assertion = new Assertion(principalName);
        var identity = new CasIdentity(assertion, "CAS");

        // Act
        var result = identity.Assertion;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(principalName, result.PrincipalName);
        Assert.Empty(result.Attributes);
    }

    [Fact]
    public void Assertion_WithAttributes_ShouldBeAccessible()
    {
        // Arrange
        var principalName = "testuser";
        var attributes = new Dictionary<string, StringValues>
        {
            ["email"] = "test@example.com",
            ["roles"] = new StringValues(["admin", "user"])
        };
        var assertion = new Assertion(principalName, attributes);
        var identity = new CasIdentity(assertion, "CAS");

        // Act
        var result = identity.Assertion;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(principalName, result.PrincipalName);
        Assert.Equal(2, result.Attributes.Count);
        Assert.Equal("test@example.com", result.Attributes["email"].ToString());
    }

    #endregion

    #region Integration with ClaimsIdentity Tests

    [Fact]
    public void CasIdentity_ShouldBeClaimsIdentity()
    {
        // Arrange
        var assertion = new Assertion("testuser");
        var identity = new CasIdentity(assertion, "CAS");

        // Act & Assert
        Assert.IsAssignableFrom<ClaimsIdentity>(identity);
    }

    [Fact]
    public void CasIdentity_WithClaims_ShouldSupportClaimsOperations()
    {
        // Arrange
        var assertion = new Assertion("testuser");
        var identity = new CasIdentity(assertion, "CAS");
        var claim = new Claim(ClaimTypes.Email, "test@example.com");

        // Act
        identity.AddClaim(claim);

        // Assert
        // Check that a claim with the same type and value exists
        Assert.True(identity.HasClaim(c => c.Type == ClaimTypes.Email && c.Value == "test@example.com"));
        Assert.True(identity.HasClaim(c => c.Type == ClaimTypes.Email));
    }

    [Fact]
    public void Name_ShouldReturnNameClaimValue_WhenNameClaimExists()
    {
        // Arrange
        var assertion = new Assertion("testuser");
        var identity = new CasIdentity(assertion, "CAS");
        var expectedName = "John Doe";
        identity.AddClaim(new Claim(ClaimsIdentity.DefaultNameClaimType, expectedName));

        // Act
        var result = identity.Name;

        // Assert
        Assert.Equal(expectedName, result);
    }

    #endregion
}

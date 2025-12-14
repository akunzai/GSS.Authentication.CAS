using System.Security.Claims;
using System.Security.Principal;
using GSS.Authentication.CAS.Security;
using NSubstitute;
using Xunit;

namespace GSS.Authentication.CAS.Core.Tests;

public class PrincipalExtensionsTests
{
    #region IIdentity.GetPrincipalName Tests

    [Fact]
    public void GetPrincipalName_WithCasIdentity_ShouldReturnPrincipalName()
    {
        // Arrange
        var principalName = "testuser";
        var assertion = new Assertion(principalName);
        var identity = new CasIdentity(assertion, "CAS");

        // Act
        var result = identity.GetPrincipalName();

        // Assert
        Assert.Equal(principalName, result);
    }

    [Fact]
    public void GetPrincipalName_WithClaimsIdentityHavingNameClaim_ShouldReturnClaimValue()
    {
        // Arrange
        var expectedName = "claimsuser";
        var identity = new ClaimsIdentity(
            [new Claim(ClaimsIdentity.DefaultNameClaimType, expectedName)],
            "TestAuth");

        // Act
        var result = identity.GetPrincipalName();

        // Assert
        Assert.Equal(expectedName, result);
    }

    [Fact]
    public void GetPrincipalName_WithClaimsIdentityWithoutNameClaim_ShouldReturnEmpty()
    {
        // Arrange
        var identity = new ClaimsIdentity(
            [new Claim("other-claim", "value")],
            "TestAuth");

        // Act
        var result = identity.GetPrincipalName();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void GetPrincipalName_WithCustomNameClaimType_ShouldReturnCorrectClaim()
    {
        // Arrange
        var expectedName = "customuser";
        var customNameType = "custom/name";
        var identity = new ClaimsIdentity(
            [new Claim(customNameType, expectedName)],
            "TestAuth",
            customNameType,
            ClaimsIdentity.DefaultRoleClaimType);

        // Act
        var result = identity.GetPrincipalName();

        // Assert
        Assert.Equal(expectedName, result);
    }

    [Fact]
    public void GetPrincipalName_WithGenericIdentity_ShouldReturnEmpty()
    {
        // Arrange
        var identity = Substitute.For<IIdentity>();
        identity.Name.Returns("mockuser");

        // Act
        var result = identity.GetPrincipalName();

        // Assert
        Assert.Empty(result);
    }

    #endregion

    #region IPrincipal.GetPrincipalName Tests

    [Fact]
    public void GetPrincipalName_WithCasPrincipal_ShouldReturnPrincipalName()
    {
        // Arrange
        var principalName = "casuser";
        var assertion = new Assertion(principalName);
        var principal = new CasPrincipal(assertion, "CAS");

        // Act
        var result = principal.GetPrincipalName();

        // Assert
        Assert.Equal(principalName, result);
    }

    [Fact]
    public void GetPrincipalName_WithClaimsPrincipalHavingNameClaim_ShouldReturnClaimValue()
    {
        // Arrange
        var expectedName = "claimsuser";
        var identity = new ClaimsIdentity(
            [new Claim(ClaimsIdentity.DefaultNameClaimType, expectedName)],
            "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = principal.GetPrincipalName();

        // Assert
        Assert.Equal(expectedName, result);
    }

    [Fact]
    public void GetPrincipalName_WithClaimsPrincipalWithMultipleIdentities_ShouldReturnFirstMatchingName()
    {
        // Arrange
        var identity1 = new ClaimsIdentity([new Claim("other", "value")], "Auth1");
        var identity2 = new ClaimsIdentity(
            [new Claim(ClaimsIdentity.DefaultNameClaimType, "seconduser")],
            "Auth2");
        var principal = new ClaimsPrincipal(identity1);
        principal.AddIdentity(identity2);

        // Act
        var result = principal.GetPrincipalName();

        // Assert
        Assert.Equal("seconduser", result);
    }

    [Fact]
    public void GetPrincipalName_WithClaimsPrincipalWithoutNameClaims_ShouldReturnEmpty()
    {
        // Arrange
        var identity = new ClaimsIdentity([new Claim("other", "value")], "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = principal.GetPrincipalName();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void GetPrincipalName_WithGenericPrincipalHavingCasIdentity_ShouldReturnPrincipalName()
    {
        // Arrange
        var principalName = "genericcasuser";
        var assertion = new Assertion(principalName);
        var identity = new CasIdentity(assertion, "CAS");
        var principal = Substitute.For<IPrincipal>();
        principal.Identity.Returns(identity);

        // Act
        var result = principal.GetPrincipalName();

        // Assert
        Assert.Equal(principalName, result);
    }

    [Fact]
    public void GetPrincipalName_WithNullIdentity_ShouldReturnEmpty()
    {
        // Arrange
        var principal = Substitute.For<IPrincipal>();
        principal.Identity.Returns((IIdentity?)null);

        // Act
        var result = principal.GetPrincipalName();

        // Assert
        Assert.Empty(result);
    }

    #endregion
}

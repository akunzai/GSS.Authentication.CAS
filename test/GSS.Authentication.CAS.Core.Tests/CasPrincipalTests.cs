using GSS.Authentication.CAS.Security;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace GSS.Authentication.CAS.Core.Tests;

public class CasPrincipalTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidAssertion_ShouldCreatePrincipal()
    {
        // Arrange
        var assertion = new Assertion("testuser");

        // Act
        var principal = new CasPrincipal(assertion, "CAS");

        // Assert
        Assert.NotNull(principal);
        Assert.Equal(assertion, principal.Assertion);
        Assert.Single(principal.Identities);
    }

    [Fact]
    public void Constructor_WithNullAssertion_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => new CasPrincipal(null!, "CAS"));
    }

    [Fact]
    public void Constructor_WithRoles_ShouldStoreThem()
    {
        // Arrange
        var assertion = new Assertion("testuser");
        var roles = new[] { "admin", "user" };

        // Act
        var principal = new CasPrincipal(assertion, "CAS", roles);

        // Assert
        Assert.True(principal.IsInRole("admin"));
        Assert.True(principal.IsInRole("user"));
    }

    #endregion

    #region IsInRole Tests

    [Fact]
    public void IsInRole_WithRoleInRolesCollection_ShouldReturnTrue()
    {
        // Arrange
        var assertion = new Assertion("testuser");
        var roles = new[] { "admin", "user" };
        var principal = new CasPrincipal(assertion, "CAS", roles);

        // Act
        var result = principal.IsInRole("admin");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsInRole_WithRoleNotInRolesCollection_ShouldReturnFalse()
    {
        // Arrange
        var assertion = new Assertion("testuser");
        var roles = new[] { "admin", "user" };
        var principal = new CasPrincipal(assertion, "CAS", roles);

        // Act
        var result = principal.IsInRole("superadmin");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsInRole_WithRoleInAssertionAttributes_ShouldReturnTrue()
    {
        // Arrange
        var attributes = new Dictionary<string, StringValues>
        {
            ["memberOf"] = new StringValues(["admin", "user"]),
            ["groups"] = new StringValues(["developers"])
        };
        var assertion = new Assertion("testuser", attributes);
        var principal = new CasPrincipal(assertion, "CAS");

        // Act
        var result = principal.IsInRole("developers");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsInRole_WithRoleInMultiValueAttribute_ShouldReturnTrue()
    {
        // Arrange
        var attributes = new Dictionary<string, StringValues>
        {
            ["roles"] = new StringValues(["role1", "role2", "role3"])
        };
        var assertion = new Assertion("testuser", attributes);
        var principal = new CasPrincipal(assertion, "CAS");

        // Act
        var result = principal.IsInRole("role2");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsInRole_WithRoleNotInAttributesOrRoles_ShouldReturnFalse()
    {
        // Arrange
        var attributes = new Dictionary<string, StringValues>
        {
            ["roles"] = new StringValues(["admin"])
        };
        var assertion = new Assertion("testuser", attributes);
        var principal = new CasPrincipal(assertion, "CAS");

        // Act
        var result = principal.IsInRole("nonexistent");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsInRole_WithNullRoles_ShouldCheckAttributesOnly()
    {
        // Arrange
        var attributes = new Dictionary<string, StringValues>
        {
            ["memberOf"] = new StringValues(["admin"])
        };
        var assertion = new Assertion("testuser", attributes);
        var principal = new CasPrincipal(assertion, "CAS", null);

        // Act
        var result = principal.IsInRole("admin");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsInRole_WithEmptyRolesAndEmptyAttributes_ShouldReturnFalse()
    {
        // Arrange
        var assertion = new Assertion("testuser");
        var principal = new CasPrincipal(assertion, "CAS", []);

        // Act
        var result = principal.IsInRole("admin");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsInRole_WithRoleInBothRolesAndAttributes_ShouldReturnTrue()
    {
        // Arrange
        var attributes = new Dictionary<string, StringValues>
        {
            ["roles"] = new StringValues(["admin"])
        };
        var assertion = new Assertion("testuser", attributes);
        var roles = new[] { "admin" };
        var principal = new CasPrincipal(assertion, "CAS", roles);

        // Act
        var result = principal.IsInRole("admin");

        // Assert
        Assert.True(result);
    }

    #endregion
}

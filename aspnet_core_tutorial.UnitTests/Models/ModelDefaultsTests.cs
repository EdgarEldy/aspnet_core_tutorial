using System.ComponentModel.DataAnnotations;
using aspnet_core_tutorial.Models;
using Xunit;

namespace aspnet_core_tutorial.UnitTests.Models;

/// <summary>
/// At this stage of the project, <see cref="Category"/>, <see cref="Product"/>,
/// <see cref="Customer"/> and <see cref="Order"/> carry no <see cref="ValidationAttribute"/>
/// beyond EF Core's own <c>[Key]</c>/<c>[Column]</c> mapping attributes (neither of which is a
/// validation attribute). These tests lock in that observed state (default property values, and
/// the fact that <see cref="Validator"/> never rejects a freshly constructed instance) so a future
/// change that silently drops or adds validation is caught, rather than pretending constraints
/// exist that the models don't actually enforce yet.
/// </summary>
public class ModelDefaultsTests
{
    [Fact]
    public void Category_Defaults_String_Property_To_Empty_And_Passes_Validation()
    {
        var category = new Category();

        Assert.Equal(string.Empty, category.CategoryName);
        Assert.True(Validator.TryValidateObject(category, new ValidationContext(category), validationResults: null, validateAllProperties: true));
    }

    [Fact]
    public void Product_Defaults_CategoryId_To_Null_Since_The_Relationship_Is_Optional()
    {
        var product = new Product();

        Assert.Null(product.CategoryId);
        Assert.Null(product.Category);
        Assert.True(Validator.TryValidateObject(product, new ValidationContext(product), validationResults: null, validateAllProperties: true));
    }

    [Fact]
    public void Customer_Defaults_All_String_Properties_To_Empty_And_Passes_Validation()
    {
        var customer = new Customer();

        Assert.Equal(string.Empty, customer.FirstName);
        Assert.Equal(string.Empty, customer.LastName);
        Assert.Equal(string.Empty, customer.Telephone);
        Assert.Equal(string.Empty, customer.Email);
        Assert.Equal(string.Empty, customer.Address);
        Assert.True(Validator.TryValidateObject(customer, new ValidationContext(customer), validationResults: null, validateAllProperties: true));
    }

    [Fact]
    public void Order_Defaults_CustomerId_And_ProductId_To_Null_Since_Both_Relationships_Are_Optional()
    {
        var order = new Order();

        Assert.Null(order.CustomerId);
        Assert.Null(order.ProductId);
        Assert.True(Validator.TryValidateObject(order, new ValidationContext(order), validationResults: null, validateAllProperties: true));
    }
}

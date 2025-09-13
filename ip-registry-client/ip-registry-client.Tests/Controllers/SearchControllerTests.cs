using Xunit;
using core_web.Areas.RelatedPatent.Controllers;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using core_web.Areas.RelatedPatent.Models;

namespace ip_registry_client.Tests.Controllers
{
    public class SearchControllerTests
    {
        [Fact]
        public void Index_ReturnsView()
        {
            // Arrange
            var controller = new SearchController();

            // Act
            var result = controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Null(viewResult.ViewName); // should render Index.cshtml by default
        }

        [Theory]
        [InlineData("blockchain")]
        [InlineData("AI")]
        public void ValidateKeyword_ValidInput_ReturnsTrue(string keyword)
        {
            // Arrange
            var controller = new SearchController();

            // Act
            var result = controller.ValidateKeyword(keyword) as JsonResult;

            // Assert
            Assert.NotNull(result);
            var data = result.Value as KeywordValidationResult;
            Assert.NotNull(data);
            Assert.True(data.Valid);
        }

        [Fact]
        public void ValidateKeyword_EmptyInput_ReturnsFalse()
        {
            // Arrange
            var controller = new SearchController();

            // Act
            var result = controller.ValidateKeyword("") as JsonResult;

            // Assert
            Assert.NotNull(result);
            var data = result.Value as KeywordValidationResult;
            Assert.NotNull(data);
            Assert.False(data.Valid);
        }
    }
}

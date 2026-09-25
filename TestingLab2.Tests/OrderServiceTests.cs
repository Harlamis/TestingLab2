using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Moq;
using Testinglab2.Variant02;

namespace TestingLab2.Tests
{
    public class OrderServiceTests
    {
        private readonly Mock<OrderService.IStockProvider> _stockMock = new();
        private readonly Mock<OrderService.ICouponValidator> _couponMock = new();
        private readonly Mock<OrderService.IOrderRepository> _repoMock = new();
        private readonly OrderService _sut;

        public OrderServiceTests()
        {
            _sut = new OrderService(_stockMock.Object, _couponMock.Object, _repoMock.Object);
        }

        [Fact]
        public void Process_StandardOrder_ReturnsCorrectCalculatedTotals()
        {
            // Arrange
            _stockMock.Setup(s => s.AvailableUnits()).Returns(10);
            var request = new OrderService.Request(Quantity: 2, UnitPrice: 1000, Premium: false, CouponClaimed: false);

            // Act
            var result = _sut.Process(request);

            // Assert
            Assert.Equal("Оформлено", result.Status);
            Assert.Equal(2000, result.Goods);
            Assert.Equal(5000, result.Delivery);
            Assert.Equal(7000, result.Total);
        }

        [Fact]
        public void Process_OrderAbove50000_HasFreeDelivery()
        {
            // Arrange
            _stockMock.Setup(s => s.AvailableUnits()).Returns(10);
            var request = new OrderService.Request(Quantity: 1, UnitPrice: 60000, Premium: false, CouponClaimed: false);

            // Act
            var result = _sut.Process(request);

            // Assert
            Assert.Equal(0, result.Delivery);
            Assert.Equal(60000, result.Total);
        }




        [Theory]
        [InlineData(true, true, true, 20)]
        [InlineData(true, false, false, 10)]
        [InlineData(false, true, true, 5)]
        [InlineData(false, true, false, 0)]
        [InlineData(false, false, false, 0)]
        public void Process_CalculatesDiscount(bool isPremium, bool couponClaimed, bool isCouponValid, int expectedDiscount)
        {
            // Arrange
            _stockMock.Setup(s => s.AvailableUnits()).Returns(10);
            _couponMock.Setup(c => c.IsValid()).Returns(isCouponValid);

            var request = new OrderService.Request(Quantity: 1, UnitPrice: 1000, Premium: isPremium, CouponClaimed: couponClaimed);

            // Act
            var result = _sut.Process(request);

            // Assert
            Assert.Equal(expectedDiscount, result.DiscountPercent);
        }



        [Fact]
        public void Process_SuccessfulOrder_SavesExactRequestAndResultToRepository()
        {
            // Arrange
            _stockMock.Setup(s => s.AvailableUnits()).Returns(10);
            var request = new OrderService.Request(Quantity: 2, UnitPrice: 1000, Premium: false, CouponClaimed: false);

            // Act
            var result = _sut.Process(request);

            // Assert
            _repoMock.Verify(r => r.Save(request, It.Is<OrderService.Result>(res => res.Status == "Оформлено")), Times.Once);
        }

        [Fact]
        public void Process_InsufficientStock_DoesNotSaveOrder()
        {
            // Arrange
            _stockMock.Setup(s => s.AvailableUnits()).Returns(1);
            var request = new OrderService.Request(Quantity: 2, UnitPrice: 1000, Premium: false, CouponClaimed: false);

            // Act
            var result = _sut.Process(request);

            // Assert
            Assert.Equal("Недостатньо товару", result.Status);
            _repoMock.Verify(r => r.Save(It.IsAny<OrderService.Request>(), It.IsAny<OrderService.Result>()), Times.Never);
        }



        [Fact]
        public void Process_NegativeStock_ThrowsInvalidOperationException()
        {
            // Arrange
            _stockMock.Setup(s => s.AvailableUnits()).Returns(-1);
            var request = new OrderService.Request(Quantity: 1, UnitPrice: 1000, Premium: false, CouponClaimed: false);

            // Assert
            Assert.Throws<InvalidOperationException>(() => _sut.Process(request));
        }

        [Fact]
        public void Process_NullRequest_ThrowsArgumentException()
        {
            // Assert
            Assert.Throws<ArgumentException>(() => _sut.Process(null!));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(20)]
        public void Process_InvalidQuantity_ThrowsArgumentException(int invalidQuantity)
        {
            // Arrange
            var request = new OrderService.Request(Quantity: invalidQuantity, UnitPrice: 1000, Premium: false, CouponClaimed: false);

            // Assert
            Assert.Throws<ArgumentException>(() => _sut.Process(request));
        }

    }
}
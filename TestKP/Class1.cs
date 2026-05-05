using DiplomMurtazin.Core;
using Xunit;

namespace TestKP
{
    public class Class1
    {
        [Theory]
        [InlineData("+7 (495) 123-45-67", true)]
        [InlineData("+7 495 1234567", true)]
        [InlineData("123", false)]
        [InlineData("", true)]
        public void IsValidPhone_WithVariousInputs_ReturnsExpectedResult(string phone, bool expected)
        {
            bool result = InputMask.IsValidPhone(phone);

            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("test@mail.ru", true)]
        [InlineData("user.name@domain.com", true)]
        [InlineData("invalid-email", false)]
        [InlineData("test@.ru", false)]
        [InlineData("", true)]
        public void IsValidEmail_WithVariousInputs_ReturnsExpectedResult(string email, bool expected)
        {

            bool result = InputMask.IsValidEmail(email);

            Assert.Equal(expected, result);

        }

        [Theory]
        [InlineData("770112345678", true)]  // 12 цифр
        [InlineData("7701123456", true)]    // 10 цифр
        [InlineData("77011234", false)]     // 8 цифр
        [InlineData("7701123456789", false)] // 13 цифр
        [InlineData("", true)]
        public void IsValidInn_WithVariousInputs_ReturnsExpectedResult(string inn, bool expected)
        {
            bool result = InputMask.IsValidInn(inn);
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("4510123456", true)]     // 10 цифр
        [InlineData("45 10 123456", true)]   // с пробелами
        [InlineData("451012345", false)]     // 9 цифр
        [InlineData("", true)]
        public void IsValidPassport_WithVariousInputs_ReturnsExpectedResult(string passport, bool expected)
        {
            bool result = InputMask.IsValidPassport(passport);

            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("8801643701125", true)]  // 13 цифр
        [InlineData("880164370112", false)]  // 12 цифр
        [InlineData("88016437011255", false)] // 14 цифр
        [InlineData("abc123", false)]
        public void IsValidBarcode_WithVariousInputs_ReturnsExpectedResult(string barcode, bool expected)
        {
            bool result = InputMask.IsValidBarcode(barcode);

            Assert.Equal(expected, result);
        }
    }
}

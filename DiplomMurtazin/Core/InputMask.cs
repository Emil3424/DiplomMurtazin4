using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DiplomMurtazin.Core
{
    public static class InputMask
    {
        #region Маски для текстовых полей

        /// <summary>
        /// Применяет маску телефонного номера +7 (999) 999-99-99
        /// </summary>
        public static void ApplyPhoneMask(TextBox textBox)
        {
            textBox.PreviewTextInput += (s, e) =>
            {
                // Разрешаем только цифры
                if (!char.IsDigit(e.Text, 0))
                {
                    e.Handled = true;
                    return;
                }

                var textBox1 = s as TextBox;
                string newText = textBox1.Text.Insert(textBox1.SelectionStart, e.Text);
                string digitsOnly = new string(newText.Where(char.IsDigit).ToArray());

                if (digitsOnly.Length > 11) // Максимум 11 цифр (код страны + номер)
                {
                    e.Handled = true;
                }
            };

            textBox.TextChanged += (s, e) =>
            {
                var textBox1 = s as TextBox;
                string digitsOnly = new string(textBox1.Text.Where(char.IsDigit).ToArray());

                if (digitsOnly.Length == 0)
                {
                    textBox1.Text = "";
                    return;
                }

                // Форматируем номер
                string formatted = "+7";
                if (digitsOnly.Length > 1) // Первая цифра - код страны
                {
                    string regionCode = digitsOnly.Substring(1, Math.Min(3, digitsOnly.Length - 1));
                    formatted += " (" + regionCode;
                }
                if (digitsOnly.Length > 4)
                {
                    string part2 = digitsOnly.Substring(4, Math.Min(3, digitsOnly.Length - 4));
                    formatted += ") " + part2;
                }
                if (digitsOnly.Length > 7)
                {
                    string part3 = digitsOnly.Substring(7, Math.Min(2, digitsOnly.Length - 7));
                    formatted += "-" + part3;
                }
                if (digitsOnly.Length > 9)
                {
                    string part4 = digitsOnly.Substring(9, Math.Min(2, digitsOnly.Length - 9));
                    formatted += "-" + part4;
                }

                // Сохраняем позицию курсора
                int caretIndex = textBox1.CaretIndex;
                textBox1.Text = formatted;
                textBox1.CaretIndex = Math.Min(caretIndex, textBox1.Text.Length);
            };
        }

        /// <summary>
        /// Применяет маску для ИНН (только цифры, 10 или 12 цифр)
        /// </summary>
        public static void ApplyInnMask(TextBox textBox, bool isLegalEntity = false)
        {
            int maxLength = isLegalEntity ? 10 : 12;

            textBox.PreviewTextInput += (s, e) =>
            {
                if (!char.IsDigit(e.Text, 0))
                {
                    e.Handled = true;
                }
            };

            textBox.PreviewKeyDown += (s, e) =>
            {
                if (e.Key == Key.Space)
                {
                    e.Handled = true;
                }
            };

            textBox.TextChanged += (s, e) =>
            {
                var textBox1 = s as TextBox;
                string digitsOnly = new string(textBox1.Text.Where(char.IsDigit).ToArray());

                if (digitsOnly.Length > maxLength)
                {
                    int caret = textBox1.CaretIndex;
                    textBox1.Text = digitsOnly.Substring(0, maxLength);
                    textBox1.CaretIndex = Math.Min(caret, textBox1.Text.Length);
                }
                else
                {
                    // Форматируем с пробелами для удобства чтения
                    if (digitsOnly.Length > 4)
                    {
                        string formatted = digitsOnly.Substring(0, 4) + " " + digitsOnly.Substring(4);
                        if (digitsOnly.Length > 8)
                        {
                            formatted = digitsOnly.Substring(0, 4) + " " +
                                       digitsOnly.Substring(4, 4) + " " +
                                       digitsOnly.Substring(8);
                        }
                        else if (digitsOnly.Length > 6)
                        {
                            formatted = digitsOnly.Substring(0, 4) + " " + digitsOnly.Substring(4);
                        }

                        int caret = textBox1.CaretIndex;
                        textBox1.Text = formatted;
                        textBox1.CaretIndex = Math.Min(caret, textBox1.Text.Length);
                    }
                }
            };
        }

        /// <summary>
        /// Применяет маску для паспортных данных (серия и номер)
        /// </summary>
        public static void ApplyPassportMask(TextBox textBox, Action<string> showHint = null)
        {
            textBox.PreviewTextInput += (s, e) =>
            {
                if (!char.IsDigit(e.Text, 0))
                {
                    e.Handled = true;
                    showHint?.Invoke("Паспорт: только цифры, формат 45 45 555555.");
                }
            };

            textBox.TextChanged += (s, e) =>
            {
                var textBox1 = s as TextBox;
                string digitsOnly = new string(textBox1.Text.Where(char.IsDigit).ToArray());

                if (digitsOnly.Length > 10) // 4 цифры серия + 6 цифр номер
                {
                    int caret = textBox1.CaretIndex;
                    textBox1.Text = digitsOnly.Substring(0, 10);
                    textBox1.CaretIndex = Math.Min(caret, textBox1.Text.Length);
                    showHint?.Invoke("Паспорт: максимум 10 цифр, формат 45 45 555555.");
                }
                else
                {
                    // Форматируем серию и номер
                    if (digitsOnly.Length > 4)
                    {
                        string formatted = digitsOnly.Substring(0, 2) + " " +
                                          digitsOnly.Substring(2, 2) + " " +
                                          digitsOnly.Substring(4);

                        int caret = textBox1.CaretIndex;
                        textBox1.Text = formatted;
                        textBox1.CaretIndex = Math.Min(caret, textBox1.Text.Length);
                    }
                }
            };
        }

        /// <summary>
        /// Применяет маску для штрих-кода (только цифры, 13 символов)
        /// </summary>
        public static void ApplyBarcodeMask(TextBox textBox, Action<string> showHint = null)
        {
            textBox.PreviewTextInput += (s, e) =>
            {
                if (!char.IsDigit(e.Text, 0))
                {
                    e.Handled = true;
                    showHint?.Invoke("Штрих-код: только цифры, 13 символов.");
                }
            };

            textBox.TextChanged += (s, e) =>
            {
                var textBox1 = s as TextBox;
                string digitsOnly = new string(textBox1.Text.Where(char.IsDigit).ToArray());

                if (digitsOnly.Length > 13)
                {
                    int caret = textBox1.CaretIndex;
                    textBox1.Text = digitsOnly.Substring(0, 13);
                    textBox1.CaretIndex = Math.Min(caret, textBox1.Text.Length);
                    showHint?.Invoke("Штрих-код: максимум 13 цифр.");
                }
            };
        }

        /// <summary>
        /// Применяет маску для числовых полей (только цифры)
        /// </summary>
        public static void ApplyNumericMask(TextBox textBox, bool allowNegative = false, bool allowDecimal = false, int maxDecimalPlaces = 2, Action<string> showHint = null)
        {
            bool isUpdating = false;

            textBox.PreviewTextInput += (s, e) =>
            {
                if (char.IsDigit(e.Text, 0))
                {
                    return;
                }

                if (allowDecimal && (e.Text == "," || e.Text == "."))
                {
                    if (textBox.Text.Contains(",") || textBox.Text.Contains("."))
                    {
                        e.Handled = true;
                        showHint?.Invoke($"Можно только один разделитель дробной части. Пример: 123.45 ({maxDecimalPlaces} знака после точки).");
                    }
                    return;
                }

                if (allowNegative && e.Text == "-" && textBox.SelectionStart == 0 && !textBox.Text.StartsWith("-"))
                {
                    return;
                }

                e.Handled = true;
                if (allowDecimal)
                {
                    showHint?.Invoke($"Введите число: только цифры, разделитель '.' или ','. Пример: 123.45 (до {maxDecimalPlaces} знаков).");
                }
                else
                {
                    showHint?.Invoke("Введите целое число: только цифры.");
                }
            };

            textBox.PreviewKeyDown += (s, e) =>
            {
                if (e.Key == Key.Space)
                {
                    e.Handled = true;
                    showHint?.Invoke("Пробелы в числе недопустимы.");
                }
            };

            DataObject.AddPastingHandler(textBox, new DataObjectPastingEventHandler((s, e) =>
            {
                if (!e.DataObject.GetDataPresent(typeof(string)))
                {
                    return;
                }

                string pastedText = e.DataObject.GetData(typeof(string)) as string ?? string.Empty;
                if (pastedText.Any(c => !char.IsDigit(c) && c != '.' && c != ',' && c != '-'))
                {
                    showHint?.Invoke("Вставляйте только числовые значения.");
                }
            }));

            textBox.TextChanged += (s, e) =>
            {
                if (isUpdating)
                {
                    return;
                }

                var tb = s as TextBox;
                if (tb == null)
                {
                    return;
                }

                int originalCaret = tb.CaretIndex;
                string originalText = tb.Text ?? string.Empty;
                string sanitized = NormalizeNumericInput(originalText, allowNegative, allowDecimal, maxDecimalPlaces);

                if (sanitized != originalText)
                {
                    isUpdating = true;
                    tb.Text = sanitized;
                    tb.CaretIndex = Math.Min(originalCaret, tb.Text.Length);
                    isUpdating = false;
                }
            };
        }

        /// <summary>
        /// Применяет маску для количества (только целые положительные числа)
        /// </summary>
        public static void ApplyQuantityMask(TextBox textBox, Action<string> showHint = null)
        {
            textBox.PreviewTextInput += (s, e) =>
            {
                if (!char.IsDigit(e.Text, 0))
                {
                    e.Handled = true;
                    showHint?.Invoke("Введите количество: только целые числа.");
                }
            };

            textBox.TextChanged += (s, e) =>
            {
                var textBox1 = s as TextBox;
                if (textBox1.Text.Length > 0)
                {
                    string digitsOnly = new string(textBox1.Text.Where(char.IsDigit).ToArray());
                    if (digitsOnly != textBox1.Text)
                    {
                        int caret = textBox1.CaretIndex;
                        textBox1.Text = digitsOnly;
                        textBox1.CaretIndex = Math.Min(caret, textBox1.Text.Length);
                    }

                    if (int.TryParse(textBox1.Text, out int value) && value < 0)
                    {
                        textBox1.Text = "0";
                    }
                }
            };
        }

        /// <summary>
        /// Применяет маску для номера документа (ФД-999999999)
        /// </summary>
        public static void ApplyFiscalDocumentMask(TextBox textBox)
        {
            textBox.TextChanged += (s, e) =>
            {
                var textBox1 = s as TextBox;
                string text = textBox1.Text;

                if (text.StartsWith("ФД-"))
                {
                    string digitsPart = text.Substring(3);
                    string digitsOnly = new string(digitsPart.Where(char.IsDigit).ToArray());

                    if (digitsOnly.Length > 9)
                    {
                        digitsOnly = digitsOnly.Substring(0, 9);
                    }

                    int caret = textBox1.CaretIndex;
                    textBox1.Text = "ФД-" + digitsOnly;
                    textBox1.CaretIndex = Math.Min(caret, textBox1.Text.Length);
                }
                else if (!string.IsNullOrEmpty(text) && !text.StartsWith("ФД-"))
                {
                    string digitsOnly = new string(text.Where(char.IsDigit).ToArray());
                    if (digitsOnly.Length > 0)
                    {
                        if (digitsOnly.Length > 9) digitsOnly = digitsOnly.Substring(0, 9);
                        textBox1.Text = "ФД-" + digitsOnly;
                    }
                }
            };
        }

        /// <summary>
        /// Применяет маску для номера договора (Д-9999-999)
        /// </summary>
        public static void ApplyContractNumberMask(TextBox textBox)
        {
            textBox.TextChanged += (s, e) =>
            {
                var textBox1 = s as TextBox;
                string text = textBox1.Text;

                if (text.StartsWith("Д-"))
                {
                    string restPart = text.Substring(2);
                    string digitsOnly = new string(restPart.Where(char.IsDigit).ToArray());

                    if (digitsOnly.Length > 7) digitsOnly = digitsOnly.Substring(0, 7);

                    string formatted = "Д-";
                    if (digitsOnly.Length > 0)
                    {
                        formatted += digitsOnly.Substring(0, Math.Min(4, digitsOnly.Length));
                        if (digitsOnly.Length > 4)
                        {
                            formatted += "-" + digitsOnly.Substring(4);
                        }
                    }

                    int caret = textBox1.CaretIndex;
                    textBox1.Text = formatted;
                    textBox1.CaretIndex = Math.Min(caret, textBox1.Text.Length);
                }
                else if (!string.IsNullOrEmpty(text) && !text.StartsWith("Д-"))
                {
                    string digitsOnly = new string(text.Where(char.IsDigit).ToArray());
                    if (digitsOnly.Length > 0)
                    {
                        string formatted = "Д-";
                        formatted += digitsOnly.Substring(0, Math.Min(4, digitsOnly.Length));
                        if (digitsOnly.Length > 4)
                        {
                            formatted += "-" + digitsOnly.Substring(4, Math.Min(3, digitsOnly.Length - 4));
                        }
                        textBox1.Text = formatted;
                    }
                }
            };
        }

        /// <summary>
        /// Применяет маску для времени (ЧЧ:ММ)
        /// </summary>
        public static void ApplyTimeMask(TextBox textBox)
        {
            textBox.PreviewTextInput += (s, e) =>
            {
                if (!char.IsDigit(e.Text, 0))
                {
                    e.Handled = true;
                }
            };

            textBox.TextChanged += (s, e) =>
            {
                var textBox1 = s as TextBox;
                string digitsOnly = new string(textBox1.Text.Where(char.IsDigit).ToArray());

                if (digitsOnly.Length > 4)
                {
                    digitsOnly = digitsOnly.Substring(0, 4);
                }

                if (digitsOnly.Length > 0)
                {
                    string formatted = digitsOnly;
                    if (digitsOnly.Length > 2)
                    {
                        int hours = int.Parse(digitsOnly.Substring(0, 2));
                        int minutes = int.Parse(digitsOnly.Substring(2));

                        if (hours > 23) hours = 23;
                        if (minutes > 59) minutes = 59;

                        formatted = hours.ToString("D2") + ":" + minutes.ToString("D2");
                    }
                    else if (digitsOnly.Length == 2)
                    {
                        int hours = int.Parse(digitsOnly);
                        if (hours > 23) hours = 23;
                        formatted = hours.ToString("D2") + ":";
                    }

                    int caret = textBox1.CaretIndex;
                    textBox1.Text = formatted;
                    textBox1.CaretIndex = Math.Min(caret, textBox1.Text.Length);
                }
            };
        }

        #endregion

        #region Валидация по регулярным выражениям

        /// <summary>
        /// Проверяет корректность email
        /// </summary>
        public static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return true; // Необязательное поле
            string pattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
            return Regex.IsMatch(email, pattern);
        }

        /// <summary>
        /// Проверяет корректность телефона
        /// </summary>
        public static bool IsValidPhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone)) return true;
            string digitsOnly = new string(phone.Where(char.IsDigit).ToArray());
            return digitsOnly.Length == 11; // +7 и 10 цифр номера
        }

        /// <summary>
        /// Проверяет корректность ИНН
        /// </summary>
        public static bool IsValidInn(string inn)
        {
            if (string.IsNullOrWhiteSpace(inn)) return true;
            string digitsOnly = new string(inn.Where(char.IsDigit).ToArray());
            return digitsOnly.Length == 10 || digitsOnly.Length == 12;
        }

        /// <summary>
        /// Проверяет корректность паспортных данных
        /// </summary>
        public static bool IsValidPassport(string passport)
        {
            if (string.IsNullOrWhiteSpace(passport)) return true;
            string digitsOnly = new string(passport.Where(char.IsDigit).ToArray());
            return digitsOnly.Length == 10; // 4 цифры серия + 6 цифр номер
        }

        /// <summary>
        /// Проверяет корректность штрих-кода
        /// </summary>
        public static bool IsValidBarcode(string barcode)
        {
            if (string.IsNullOrWhiteSpace(barcode)) return false;
            string digitsOnly = new string(barcode.Where(char.IsDigit).ToArray());
            return digitsOnly.Length == 13;
        }

        /// <summary>
        /// Проверяет корректность цены
        /// </summary>
        public static bool IsValidPrice(decimal price)
        {
            return price >= 0;
        }

        /// <summary>
        /// Проверяет корректность количества
        /// </summary>
        public static bool IsValidQuantity(int quantity)
        {
            return quantity > 0;
        }

        #endregion

        #region Вспомогательные методы

        /// <summary>
        /// Очищает строку от всех нецифровых символов
        /// </summary>
        public static string CleanDigits(string input)
        {
            return new string(input?.Where(char.IsDigit).ToArray() ?? Array.Empty<char>());
        }

        /// <summary>
        /// Форматирует дату для отображения
        /// </summary>
        public static string FormatDate(DateTime? date)
        {
            return date?.ToString("dd.MM.yyyy") ?? "";
        }

        /// <summary>
        /// Форматирует дату и время для отображения
        /// </summary>
        public static string FormatDateTime(DateTime? dateTime)
        {
            return dateTime?.ToString("dd.MM.yyyy HH:mm") ?? "";
        }

        private static string NormalizeNumericInput(string input, bool allowNegative, bool allowDecimal, int maxDecimalPlaces)
        {
            if (string.IsNullOrEmpty(input))
            {
                return string.Empty;
            }

            input = input.Replace(',', '.');

            bool hasNegative = false;
            bool hasDecimalPoint = false;
            string integerPart = string.Empty;
            string fractionPart = string.Empty;

            for (int i = 0; i < input.Length; i++)
            {
                char c = input[i];

                if (char.IsDigit(c))
                {
                    if (hasDecimalPoint && allowDecimal)
                    {
                        fractionPart += c;
                    }
                    else
                    {
                        integerPart += c;
                    }

                    continue;
                }

                if (allowNegative && c == '-' && i == 0 && !hasNegative)
                {
                    hasNegative = true;
                    continue;
                }

                if (allowDecimal && c == '.' && !hasDecimalPoint)
                {
                    hasDecimalPoint = true;
                }
            }

            if (allowDecimal && maxDecimalPlaces >= 0 && fractionPart.Length > maxDecimalPlaces)
            {
                fractionPart = fractionPart.Substring(0, maxDecimalPlaces);
            }

            if (string.IsNullOrEmpty(integerPart) && hasDecimalPoint)
            {
                integerPart = "0";
            }

            string sign = hasNegative ? "-" : string.Empty;

            if (!allowDecimal)
            {
                return sign + integerPart;
            }

            return hasDecimalPoint
                ? $"{sign}{integerPart}.{fractionPart}"
                : $"{sign}{integerPart}";
        }

        #endregion
    }
}
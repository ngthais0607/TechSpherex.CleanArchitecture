using SnpDepotManagement.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace SnpDepotManagement.Domain.ValueObjects;

public sealed record ContainerNumber
{
    // Chuỗi số container hoàn chỉnh (chuẩn hóa dạng in hoa).
    public string Value { get; }

    // 3 ký tự đầu: Mã định danh chủ sở hữu/hãng tàu (Owner Code)[cite: 4].
    public string OwnerCode => Value[..3];

    // Ký tự thứ 4: Mã loại container (Type Code)[cite: 4].
    public char TypeCode => Value[3];

    // 6 ký tự tiếp theo: Số sê-ri duy nhất do hãng cấp (Serial Number)[cite: 4].
    public string SerialNumber => Value.Substring(4, 6);

    // Ký tự số cuối cùng: Chữ số kiểm tra (Check Digit) tính theo Modulo 11[cite: 4, 5].
    public int CheckDigit => Value[^1] - '0';

    private ContainerNumber(string value) => Value = value;

    public static Result Create(String? rawInput)
    {
        // 1. Kiểm tra chuỗi rỗng
        if (string.IsNullOrWhiteSpace(rawInput))
            return Result.Failure(Error.Validation("Container.Empty", "Số container không được để trống."));

        var normalized = rawInput.Trim().ToUpperInvariant();

        // 2. Dùng biểu thức chính quy (Regex) xác thực cấu trúc: 4 chữ cái + 7 chữ số[cite: 4]
        if (!Regex.IsMatch(normalized, @"^[A-Z]{4}\d{7}$"))
        {
            return Result.Failure(
                Error.Validation("Container.InvalidFormat", "Số container phải gồm 4 chữ cái và 7 chữ số (Ví dụ: CMAU1234567)."));
        }

        // 3. Tính toán và so khớp thuật toán số kiểm tra Modulo 11[cite: 5]
        if (!IsValidCheckDigit(normalized))
        {
            return Result.Failure(
                Error.Validation("Container.InvalidCheckDigit", "Mã kiểm tra (Check digit) không khớp theo thuật toán Modulo 11."));
        }

        return Result.Success(new ContainerNumber(normalized));
    }

    /// Thuật toán kiểm tra chữ số kiểm tra (Check Digit) theo chuẩn ISO 6346 / Modulo 11[cite: 4, 5].
    private static bool IsValidCheckDigit(string number)
    {
        int sum = 0;
        // Duyệt qua 10 ký tự đầu tiên (4 chữ cái + 6 số sê-ri)
        for (int i = 0; i < 10; i++)
        {
            char c = number[i];
            // Nếu là ký tự chữ cái thì quy đổi sang số theo bảng ISO; nếu là số thì trừ '0' để lấy giá trị số nguyên
            int charValue = char.IsLetter(c) ? GetLetterValue(c) : (c - '0');

            // Phép dịch bit '(1 << i)' tương đương 2^i: trọng số vị trí tăng dần theo lũy thừa của 2
            sum += charValue * (1 << i);
        }

        // Lấy số dư khi chia tổng cho 11
        int remainder = sum % 11;

        // Theo chuẩn ISO, nếu phần dư là 10 thì số kiểm tra quy ước là 0 (hoặc ký tự tương đương)[cite: 5]
        int expectedDigit = remainder == 10 ? 0 : remainder;

        // Chữ số kiểm tra thực tế nằm ở vị trí thứ 11 (index 10)
        int actualDigit = number[10] - '0';

        return expectedDigit == actualDigit;
    }

    /// 
    /// Bảng quy đổi ký tự chữ sang giá trị số theo tiêu chuẩn ISO 6346.
    /// Lưu ý: Bỏ qua các giá trị là bội số của 11 (11, 22, 33).
    /// 
    private static int GetLetterValue(char letter)
    {
        int index = letter - 'A' + 10; // A=10, B=12...
        if (index >= 11) index++;      // Bỏ qua số 11
        if (index >= 22) index++;      // Bỏ qua số 22
        if (index >= 33) index++;      // Bỏ qua số 33
        return index;
    }

    // Ghi đè ToString để in ra chuỗi số container gốc
    public override string ToString() => Value;
}
    


